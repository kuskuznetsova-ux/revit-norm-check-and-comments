using System.Collections.Generic;

namespace RevitNormCheck.Checks
{
    public interface ICheck
    {
        string Id { get; }
        void Run(CheckContext context, CheckDefinition definition, List<Finding> findings);
    }
}
