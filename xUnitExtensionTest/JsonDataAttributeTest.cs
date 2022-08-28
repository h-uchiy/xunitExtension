using System.Data;
using Newtonsoft.Json.Linq;
using xUnitExtension;

namespace xUnitExtensionTest;

public class JsonDataAttributeTest
{
    #region Resouce Finding Tests

    [Theory]
    [JsonData(FileName = "FileName.json")]
    public void LoadFromFileTest(int index, string value)
    {
        Assert.Equal(1, index);
        Assert.Equal("LoadFromFileTestValue", value);
    }

    [Theory]
    [JsonData(FileName = "xUnitExtensionTest.ResourceName.json")]
    public void LoadFromSpecifiedResourceTest(int index, string value)
    {
        Assert.Equal(1, index);
        Assert.Equal("LoadFromSpecifiedResourceTestValue", value);
    }

    [Theory]
    [JsonData]
    public void LoadFromDefaultResourceTest(int index, string value)
    {
        Assert.Equal(1, index);
        Assert.Equal("LoadFromDefaultResourceTestValue", value);
    }

    [Fact]
    public void ResourceNotFoundTest()
    {
        // Arrange
        var sut = new JsonDataAttribute() { FileName = "NotExistResource" };
        var methodInfo = this.GetType().GetMethod(nameof(ResourceNotFoundTest))!;
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => sut.GetData(methodInfo));
    }

    [Theory]
    [JsonData("KeyName")]
    public void KeyTest(int index, string value)
    {
        Assert.Equal(1, index);
        Assert.Equal("KeyTestValue", value);
    }

    [Fact]
    public void KeyNotFoundTest()
    {
        // Arrange
        var sut = new JsonDataAttribute("NotExistKeyName");
        var methodInfo = this.GetType().GetMethod(nameof(ResourceNotFoundTest))!;
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => sut.GetData(methodInfo));
    }

    #endregion

    [Theory]
    [JsonData]
    public void FromArrayTest(int index, string value)
    {
        Assert.Equal($"FromArrayTestValue{index}", value);
    }

    [Theory]
    [JsonData]
    public void FromObjectTest(int index, string value)
    {
        Assert.Equal(1, index);
        Assert.Equal("FromObjectTestValue", value);
    }

    [Theory]
    [JsonData]
    public void VariousParameterTypesTest(string param1, long param2, dynamic param3,
        TestData testData, DataTable dataTable,
        JArray jArray, JObject jObject, JValue jValue)
    {
        var expectedTestData = new TestData()
        {
            IntProperty = 3421,
            StringProperty = "TestDataStringPropertyValue"
        };

        Assert.Equal("param1", param1);

        Assert.Equal(1234567890123456789L, param2);

        Assert.Equal(541, (int)param3.IntProperty.Value);
        Assert.Equal("StringPropertyValue", (string)param3.StringProperty.Value);

        Assert.Equal(expectedTestData, testData, TestData.Comparer);

        Assert.Equal("Index", dataTable.Columns[0].ColumnName);
        Assert.Equal("Column", dataTable.Columns[1].ColumnName);
        Assert.All(dataTable.AsEnumerable(), (row, i) =>
        {
            Assert.Equal(i + 1L, row.ItemArray[0]);
            Assert.Equal($"Value{i + 1}", row.ItemArray[1]);
        });

        Assert.Equal(10, jArray[0].Value<int>());
        Assert.Equal("Value20", jArray[1].Value<string>());

        Assert.Equal(100, jObject["Property1"]?.Value<int>());
        Assert.Equal("Value200", jObject["Property2"]?.Value<string>());

        Assert.Equal("jValueValue", jValue.Value<string>());
    }

    [Theory]
    [JsonData(1, "FromInlineData1")]
    [JsonData(2, "FromInlineData2")]
    public void InlineDataWithObjectTest(int index, string value, int fromJsonIndex, string fromJsonValue)
    {
        Assert.Equal($"FromInlineData{index}", value);
        Assert.Equal(101, fromJsonIndex);
        Assert.Equal("InlineDataTestValue", fromJsonValue);
    }

    [Theory]
    [JsonData(1, "FromInlineData1")]
    [JsonData(2, "FromInlineData2")]
    public void InlineDataWithArrayTest(int index, string value, int fromJsonIndex, string fromJsonValue)
    {
        Assert.Equal($"FromInlineData{index}", value);
        Assert.Equal(101, fromJsonIndex);
        Assert.Equal("InlineDataTestValue", fromJsonValue);
    }

    public class TestData
    {
        private sealed class TestDataEqualityComparer : IEqualityComparer<TestData>
        {
            public bool Equals(TestData x, TestData y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.IntProperty == y.IntProperty && x.StringProperty == y.StringProperty &&
                       x.UnspecifiedProperty == y.UnspecifiedProperty;
            }

            public int GetHashCode(TestData obj)
            {
                return HashCode.Combine(obj.IntProperty, obj.StringProperty, obj.UnspecifiedProperty);
            }
        }

        public static IEqualityComparer<TestData> Comparer { get; } = new TestDataEqualityComparer();

        public int IntProperty { get; set; }
        public string? StringProperty { get; set; }
        public long? UnspecifiedProperty { get; set; }
    }
}