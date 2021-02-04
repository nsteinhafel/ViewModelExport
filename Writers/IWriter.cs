using System;

namespace ViewModelExport.Writers
{
    public interface IWriter
    {
        string Process(Type t);

        string Name(Type t);

        string Extension { get; }
    }
}