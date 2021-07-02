﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

bool? outputAsBinary = null;
string outpath = null;
string inpath = null;
var simplify = false;
Argument? arg = null;
var old = false;
var offset = Vector3.Zero;

foreach (var flag in args)
{
    switch (arg)
    {
        case Argument.Input:
            {
                inpath = flag;
                arg = null;
                break;
            }
        case Argument.Output:
            {
                outpath = flag;
                arg = null;
                break;
            }
        case Argument.Offset:
            {
                var axis = flag.Split(",").Select(a => float.Parse(a, SaveData.us)).ToArray();
                offset += new Vector3(axis[0], axis[1], axis[2]);
                arg = null;
                break;
            }
        case Argument.Up:
            {
                offset += Vector3.UnitY * float.Parse(flag, SaveData.us);
                arg = null;
                break;
            }
        case null:
            {
                switch (flag.ToLower())
                {
                    case "-ascii":
                    case "-a":
                        {
                            outputAsBinary = false;
                            break;
                        }
                    case "-binary":
                    case "-bin":
                    case "-b":
                        {
                            outputAsBinary = true;
                            break;
                        }
                    case "-simplify":
                    case "-s":
                        {
                            simplify = true;
                            break;
                        }
                    case "-input":
                    case "-i":
                        {
                            arg = Argument.Input;
                            break;
                        }
                    case "-output":
                    case "-o":
                        {
                            arg = Argument.Output;
                            break;
                        }
                    case "-old":
                        {
                            old = true;
                            break;
                        }
                    case "-offset":
                        {
                            arg = Argument.Offset;
                            break;
                        }
                    case "-up":
                        {
                            arg = Argument.Up;
                            break;
                        }
                }
                break;
            }
    }
}
if (!outputAsBinary.HasValue)
{
    Console.Error.WriteLine("Invalid arguments: no output encoding specified");
    return;
}
if (inpath == null)
{
    Console.Error.WriteLine("Invalid arguments: no input file specified");
    return;
}
if (outpath == null)
{
    Console.Error.WriteLine("Invalid arguments: no output file specified");
    return;
}

SaveData data;

try
{
    using var infile = File.Open(inpath, FileMode.Open);
    data = new SaveData(infile, old);
}
catch (Exception ex)
{
    Console.Error.WriteLine("The input file could not be loaded");
    Console.Error.WriteLine(ex);
    return;
}


if (simplify)
{
    var additions = new Dictionary<int, SaveData.AddItem>();
    var remove = new List<(SaveData.AddItem add, SaveData.DestroyItem destroy)>();
    foreach (var entry in data.save)
    {
        switch (entry)
        {
            case SaveData.AddItem add:
                additions[add.objectId] = add;
                break;
            case SaveData.DestroyItem destroy:
                if (additions.ContainsKey(destroy.objectId)) remove.Add((additions[destroy.objectId], destroy));
                break;
        }
    }
    foreach (var entry in remove)
    {
        data.save.Remove(entry.add);
        data.save.Remove(entry.destroy);
    }
}

foreach (var entry in data.save)
{
    switch (entry)
    {
        case SaveData.AddItem add:
            add.position += offset;
            break;
    }
}


using var outfile = File.Open(outpath, FileMode.OpenOrCreate);
outfile.SetLength(0);

if (outputAsBinary.Value)
{
    using var writer = new BinaryWriter(outfile);
    data.ToBinary(writer);
}
else
{
    using var writer = new StreamWriter(outfile);
    data.ToASCII(writer);
}

enum Argument
{
    Input,
    Output,
    Offset,
    Up,
}