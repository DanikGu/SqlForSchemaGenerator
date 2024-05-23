﻿using SqlForSchemaGenerator.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlForSchemaGenerator.Core.Interfaces
{
    public interface ISqlTypesConverter
    {
        string GetSqlType(SystemTypesEnum dbType);
        SystemTypesEnum GetSystemType(string dbType);
    }
}
