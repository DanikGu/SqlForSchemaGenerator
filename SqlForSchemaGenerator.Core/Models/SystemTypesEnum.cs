using System.ComponentModel;

namespace SqlForSchemaGenerator.Core.Models;
public enum SystemTypesEnum
{
  [Description("Integer")]
  Integer,
  [Description("Small Integer")]
  SmallInteger,
  [Description("Big Integer")]
  BigInteger,
  [Description("Decimal")]
  Decimal,
  [Description("Big Integer")]
  Real,
  [Description("Big Integer")]
  Double,
  [Description("Unlimited Text")]
  UnlimitedText,
  [Description("Limited Text")]
  LimitedText,
  [Description("Date")]
  Date,
  [Description("Time")]
  Time,
  [Description("Timestamp")]
  Timestamp,
  [Description("Boolean")]
  Boolean,
  [Description("Binary")]
  Binary,
  [Description("UUID")]
  UUID
}
