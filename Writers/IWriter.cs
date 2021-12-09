using System;

namespace ViewModelExport.Writers;

public interface IWriter
{
    string Extension { get; }

    string Process(Type t);

    string Name(Type t);
}