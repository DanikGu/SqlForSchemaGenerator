using SqlForSchemaGenerator.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlForSchemaGenerator.Core.Interfaces
{
    public interface ISqlGenerator
    {
        public string GetSql(DiffChecker checker);
    }
}
