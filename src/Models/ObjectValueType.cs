namespace PSLauncher.Models;

public enum ObjectValueType
{
    [Icon("NullTypeIcon")]
    Null,
    [Icon("StringTypeIcon")]
    String,
    [Icon("NumberTypeIcon")]
    Number,
    [Icon("BooleanTypeIcon")]
    Boolean,
    [Icon("EnumValueTypeIcon")]
    Enum,
    [Icon("StructTypeIcon")]
    Struct,
    [Icon("ClassTypeIcon")]
    Class,
    [Icon("ExceptionTypeIcon")]
    Exception,
    [Icon("DirectoryTypeIcon")]
    Directory,
    [Icon("FileTypeIcon")]
    File,
    [Icon("CollectionTypeIcon")]
    Collection,
    [Icon("DictionaryTypeIcon")]
    Dictionary,
    [Icon("TemporalTypeIcon")]
    Temporal,
}
