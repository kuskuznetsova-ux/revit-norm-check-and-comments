using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RevitNormCheck.Corpus
{
    [DataContract]
    public class CorpusFile
    {
        // Версия разбора: при её изменении корпус пересобирается заново, а не берётся из кэша.
        [DataMember(Name = "version")] public int Version { get; set; }
        [DataMember(Name = "built")] public string Built { get; set; }
        [DataMember(Name = "documents")] public List<CorpusDocument> Documents { get; set; } = new List<CorpusDocument>();
    }

    [DataContract]
    public class CorpusDocument
    {
        [DataMember(Name = "name")] public string Name { get; set; }
        // Путь относительно папки с нормами.
        [DataMember(Name = "path")] public string Path { get; set; }
        [DataMember(Name = "sizeBytes")] public long SizeBytes { get; set; }
        [DataMember(Name = "modifiedUtcTicks")] public long ModifiedUtcTicks { get; set; }
        [DataMember(Name = "error")] public string Error { get; set; }
        [DataMember(Name = "clauses")] public List<CorpusClause> Clauses { get; set; } = new List<CorpusClause>();
    }

    [DataContract]
    public class CorpusClause
    {
        [DataMember(Name = "number")] public string Number { get; set; }
        [DataMember(Name = "page")] public int Page { get; set; }
        [DataMember(Name = "text")] public string Text { get; set; }
    }
}
