using CSharpToTypeScript.Core.DependencyInjection;
using CSharpToTypeScript.Core.Options;
using CSharpToTypeScript.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CSharpToTypeScript.Core.Tests
{
    public class CodeConverterShould
    {
        private readonly ICodeConverter _codeConverter;

        public CodeConverterShould()
        {
            var serviceProvider = new ServiceCollection()
                .AddCSharpToTypeScript()
                .BuildServiceProvider();

            _codeConverter = serviceProvider.GetRequiredService<ICodeConverter>();
        }

        [Fact]
        public void ConvertClass()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    id: number;
    name: string;
}", converted);
        }

        [Fact]
        public void ConvertInterface()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"interface IItem
{
    int Id { get; set; }
    string Name { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    id: number;
    name: string;
}", converted);
        }

        [Fact]
        public void ConvertEnum()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"enum Color
{
    Red, Blue
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export enum Color {
    Red,
    Blue
}", converted);
        }

        [Fact]
        public void ConvertGenericType()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item<T>
{
    public int Id { get; set; }
    public T SomeT { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item<T> {
    id: number;
    someT: T;
}", converted);
        }

        [Fact]
        public void ConvertMultipleTypes()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"interface IItem
{
    int Id { get; set; }
    string Name { get; set; }
}

class MyItem : IITem
{
    public int Id { get; set; }
    public string Name { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    id: number;
    name: string;
}

export interface MyItem {
    id: number;
    name: string;
}", converted);
        }

        [Fact]
        public void SupportInheritance()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"interface IItem : IItemInterfaceBase
{
    string Name { get; set;}
}                
                
class DerivedItem : ItemBase, IItem
{
    public int Id { get; set; }
    private string Name { get; set; }
}

class ImplementingItem : IItem
{
    public int Id { get; set; }
    private string Name { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item extends ItemInterfaceBase {
    name: string;
}

export interface DerivedItem extends ItemBase {
    id: number;
}

export interface ImplementingItem {
    id: number;
}", converted);
        }

        [Fact]
        public void IgnoreNotSerializableMembers()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int Id { get; set; }
    private string Name { get; set; }
    public static int Count { get; set; }
    public int Number { private get; set; }

    public int Foo()
    {
        return 0;
    }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    id: number;
}", converted);
        }

        [Fact]
        public void IgnoreStaticTypes()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"public static class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(string.Empty, converted);
        }

        [Fact]
        public void ConvertBasicTypes()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int Integer { get; set; }
    public double Double { get; set; }
    public string String { get; set; }
    public char Character { get; set; }
    public DateTime Date { get; set; }
    public Guid Guid { get; set; }
    public bool Boolean { get; set; } 
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    integer: number;
    double: number;
    string: string;
    character: string;
    date: string;
    guid: string;
    boolean: boolean;
}", converted);
        }

        [Fact]
        public void ConvertTuples()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public (int, int, string) TupleA { get; set; }
    public (int id, string name) TupleB { get; set; }
    public Tuple<int, string> TupleC { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    tupleA: { item1: number; item2: number; item3: string; };
    tupleB: { id: number; name: string; };
    tupleC: { item1: number; item2: string; };
}", converted);
        }

        [Fact]
        public void ConvertDictionaries()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public Dictionary<string, int> Dict { get; set; }
    public Dictionary<bool, string> IllegalDict { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    dict: { [key: string]: number; };
    illegalDict: Dictionary<boolean, string>;
}", converted);
        }

        [Fact]
        public void ConvertCollections()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int[] Array { get; set; }
    public string[,] Array2D { get; set; }
    public IEnumerable<string> Enumerable { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    array: number[];
    array2D: string[][];
    enumerable: string[];
}", converted);
        }

        [Fact]
        public void ConvertNullableTypes()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int? Id { get; set; }
    public IEnumerable<int?> Collection { get; set; }
    public Nullable<float> Generic { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4, default, NullableOutputType.Undefined));

            Assert.Equal(@"export interface Item {
    id?: number;
    collection: (number | undefined)[];
    generic?: number;
}", converted);
        }

        [Fact]
        public void ConvertComplexTypes()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public IEnumerable<Dictionary<int, (string, int?)>?>? Wtf { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    wtf: ({ [key: number]: { item1: string; item2: number | null; }; } | null)[] | null;
}", converted);
        }

        [Fact]
        public void RespectIndentationSettings()
        {
            const string code = @"class Item
{
    public int MyProperty { get; set; }
};";

            var twoSpaceIndented = _codeConverter.ConvertToTypeScript(
                code, new CodeConversionOptions(export: true, useTabs: false, tabSize: 2));

            var tabIndented = _codeConverter.ConvertToTypeScript(
                code, new CodeConversionOptions(export: true, useTabs: true));

            Assert.Equal(@"export interface Item {
  myProperty: number;
}", twoSpaceIndented);

            Assert.Equal(@"export interface Item {
" + "\t" + @"myProperty: number;
}", tabIndented);
        }

        [Fact]
        public void RespectExportSettings()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int Id => 4;
}", new CodeConversionOptions(export: false, useTabs: false, tabSize: 4));

            Assert.Equal(@"interface Item {
    id: number;
}", converted);
        }

        [Fact]
        public void LetYouChooseDateOutputType()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public DateTime CreatedAt { get; set; }
}", new CodeConversionOptions(export: false, useTabs: false, tabSize: 4, convertDatesTo: DateOutputType.Date));

            Assert.Equal(@"interface Item {
    createdAt: Date;
}", converted);

            converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public IEnumerable<DateTimeOffset> Dates { get; set; }
}", new CodeConversionOptions(export: false, useTabs: false, tabSize: 4, convertDatesTo: DateOutputType.Union));

            Assert.Equal(@"interface Item {
    dates: (string | Date)[];
}", converted);
        }

        [Fact]
        public void RespectCasingSettings()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int Id => 4;
    public (int, int) Tuple { get; set; }
}", new CodeConversionOptions(export: false, useTabs: false, tabSize: 4, toCamelCase: false));

            Assert.Equal(@"interface Item {
    Id: number;
    Tuple: { Item1: number; Item2: number; };
}", converted);
        }

        [Fact]
        public void RespectInterfacePrefixSettings()
        {
            const string source = "interface IItem { }";

            var converted = _codeConverter.ConvertToTypeScript(source, new CodeConversionOptions(export: false, useTabs: false, tabSize: 4, toCamelCase: false, removeInterfacePrefix: true));

            Assert.Equal(@"interface Item {

}", converted);

            converted = _codeConverter.ConvertToTypeScript(source, new CodeConversionOptions(export: false, useTabs: false, tabSize: 4, toCamelCase: false, removeInterfacePrefix: false));

            Assert.Equal(@"interface IItem {

}", converted);
        }

        [Fact]
        public void GenerateImportStatements()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public ImportMe MyProperty { get; set; }
}", new CodeConversionOptions(export: false, useTabs: true, importGenerationMode: ImportGenerationMode.Simple));

            Assert.StartsWith("import { ImportMe } from \"./importMe\";", converted);
        }

        [Fact]
        public void NotImportGenericTypeParameters()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item<T>
{
    public T MyProperty { get; set; }
}", new CodeConversionOptions(export: false, useTabs: true, importGenerationMode: ImportGenerationMode.Simple));

            Assert.DoesNotContain("import { T }", converted);
        }

        [Fact]
        public void RespectModuleNameSettingsWhenGeneratingImports()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public ImportMe MyProperty { get; set; }
}", new CodeConversionOptions(export: false, useTabs: true, importGenerationMode: ImportGenerationMode.Simple,
            useKebabCase: true, appendModelSuffix: true));

            Assert.StartsWith("import { ImportMe } from \"./import-me.model\";", converted);
        }

        [Fact]
        public void RespectSingleQuoteSetting()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public ImportMe MyProperty { get; set; }
}", new CodeConversionOptions(export: false, useTabs: true,
            importGenerationMode: ImportGenerationMode.Simple, quotationMark: QuotationMark.Single));

            Assert.StartsWith("import { ImportMe } from './importMe';", converted);
        }

        [Fact]
        public void NotRemovePrefixFromGenericTypeParameters()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item<ITest>
{
    public ITest MyProperty { get; set; }
    public Generic<ITest> MyOtherProperty { get; set; }
}", new CodeConversionOptions(export: false, useTabs: true, removeInterfacePrefix: true));

            Assert.Contains("interface Item<ITest>", converted);
            Assert.Contains("myProperty: ITest", converted);
            Assert.Contains("myOtherProperty: Generic<ITest>", converted);
        }

        [Fact]
        public void ConvertPublicFields()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int publicField, otherPublicField;
    public string text;
    public const bool isSomething;
    private string privateField;
}", new CodeConversionOptions(export: false, useTabs: true));

            Assert.Contains("publicField: number;", converted);
            Assert.Contains("otherPublicField: number;", converted);
            Assert.Contains("text: string;", converted);
            Assert.Contains("isSomething: boolean;", converted);
            Assert.DoesNotContain("privateField", converted);
        }

        [Fact]
        public void PreserverMemberOrder()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int firstField;
    public int Property { get; set; };
    public int secondField;
}", new CodeConversionOptions(export: false, useTabs: false, tabSize: 4));

            Assert.Contains(@"firstField: number;
    property: number;
    secondField: number;", converted);
        }

        [Fact]
        public void IgnoreMembersMarkedWithJsonIgnore()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int Id { get; set; }

    [SomeOtherAttribute]
    public string FirstName { get; set; }

    [JsonIgnore]
    public string LastName { get; set; }

    [SomeOtherAttribute, JsonIgnore]
    public int Count { get; set; }

    [SomeOtherAttribute]
    [JsonIgnore]
    public int SomeRandomNumber { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    id: number;
    firstName: string;
}", converted);
        }

        [Fact]
        public void UseNameFromJsonPropertyNameAttribute()
        {
            var converted = _codeConverter.ConvertToTypeScript(@"class Item
{
    public int Id { get; set; }

    [JsonPropertyName(name: ""Name"")]
    public string FirstName { get; set; }

    [SomeOtherAttribute, JsonPropertyName(""How_Many""))]
    public int Count { get; set; }

    [SomeOtherAttribute]
    [JsonPropertyName(name: ""Random"")]
    public int SomeRandomNumber { get; set; }

    [JsonPropertyName(""^_^"")]
    public string InvalidName { get; set; }

    [JsonPropertyName(@""¯\_(ツ)_/¯"")]
    public string AnotherInvalidName { get; set; }

    [JsonPropertyName(""¯\\_(シ)_/¯"")]
    public string OneMore { get; set; }
}", new CodeConversionOptions(export: true, useTabs: false, tabSize: 4));

            Assert.Equal(@"export interface Item {
    id: number;
    Name: string;
    How_Many: number;
    Random: number;
    ""^_^"": string;
    ""¯\_(ツ)_/¯"": string;
    ""¯\_(シ)_/¯"": string;
}", converted);
        }
    }
}
