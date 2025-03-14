using Microsoft.Extensions.Logging.Abstractions;
using Nexus.DataModel;
using Nexus.Extensibility;
using System.Runtime.InteropServices;
using System.Text.Json;
using Xunit;

namespace Nexus.Sources.Tests;

using MySettings = StructuredFileDataSourceSettings<GantnerSettings, GantnerAdditionalFileSourceSettings>;

public class GantnerTests
{
    [Fact]
    public async Task ProvidesCatalog()
    {
        // arrange
        var dataSource = (IDataSource<MySettings>)new Gantner();
        var context = BuildContext();

        await dataSource.SetContextAsync(context, NullLogger.Instance, CancellationToken.None);

        // act
        var actual = await dataSource.EnrichCatalogAsync(new("/A/B/C"), CancellationToken.None);
        var actualIds = actual.Resources!.Select(resource => resource.Id).ToList();
        var actualUnits = actual.Resources!.Select(resource => resource.Properties?.GetStringValue("unit")).ToList();
        var actualGroups = actual.Resources!.SelectMany(resource => resource.Properties?.GetStringArray("groups")!).ToList();
        var (begin, end) = await dataSource.GetTimeRangeAsync("/A/B/C", CancellationToken.None);

        // assert
        var expectedIds = new List<string>() { "WEA10_ACC_Y", "WEA10_ACC_Z" };
        var expectedUnits = new List<string>() { " V", " V" };
        var expectedGroups = new List<string>() { "group-A", "group-A" };
        var expectedStartDate = new DateTime(2015, 12, 10, 00, 00, 00, DateTimeKind.Utc);
        var expectedEndDate = new DateTime(2015, 12, 10, 00, 20, 00, DateTimeKind.Utc);

        Assert.True(expectedIds.SequenceEqual(actualIds));
        Assert.True(expectedUnits.SequenceEqual(actualUnits));
        Assert.True(expectedGroups.SequenceEqual(actualGroups));
        Assert.Equal(expectedStartDate, begin);
        Assert.Equal(expectedEndDate, end);
    }

    [Fact]
    public async Task ProvidesDataAvailability()
    {
        // arrange
        var dataSource = (IDataSource<MySettings>)new Gantner();
        var context = BuildContext();

        await dataSource.SetContextAsync(context, NullLogger.Instance, CancellationToken.None);

        // act
        var actual = new Dictionary<DateTime, double>();
        var begin = new DateTime(2015, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2016, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        var currentBegin = begin;

        while (currentBegin < end)
        {
            actual[currentBegin] = await dataSource.GetAvailabilityAsync("/A/B/C", currentBegin, currentBegin.AddDays(1), CancellationToken.None);
            currentBegin += TimeSpan.FromDays(1);
        }

        // assert
        var expected = new SortedDictionary<DateTime, double>(Enumerable.Range(0, 31).ToDictionary(
            i => begin.AddDays(i),
            i => 0.0))
        {
            [begin.AddDays(9)] = 2 / 144.0
        };

        Assert.True(expected.SequenceEqual(new SortedDictionary<DateTime, double>(actual)));
    }

    [Fact]
    public async Task CanReadFullDay()
    {
        // arrange
        var dataSource = (IDataSource<MySettings>)new Gantner();
        var context = BuildContext();

        await dataSource.SetContextAsync(context, NullLogger.Instance, CancellationToken.None);

        // act
        var catalog = await dataSource.EnrichCatalogAsync(new("/A/B/C"), CancellationToken.None);
        var resource = catalog.Resources![0];
        var representation = resource.Representations![0];
        var catalogItem = new CatalogItem(catalog, resource, representation, default);

        var begin = new DateTime(2015, 12, 10, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2015, 12, 11, 0, 0, 0, DateTimeKind.Utc);
        var (data, status) = ExtensibilityUtilities.CreateBuffers(representation, begin, end);

        var result = new ReadRequest(resource.Id, catalogItem, data, status);
        await dataSource.ReadAsync(begin, end, [result], default!, new Progress<double>(), CancellationToken.None);

        // assert
        void DoAssert()
        {
            var data = MemoryMarshal.Cast<byte, float>(result.Data.Span);

            Assert.Equal(4.9149, data[0], precision: 4);
            Assert.Equal(4.9613, data[10], precision: 4);
            Assert.Equal(5.0681, data[100], precision: 4);
            Assert.Equal(4.9112, data[1000], precision: 4);
            Assert.Equal(5.0109, data[10000], precision: 4);

            Assert.Equal(1, result.Status.Span[14999]);
            Assert.Equal(0, result.Status.Span[30000]);
        }

        DoAssert();
    }

    private static DataSourceContext<MySettings> BuildContext()
    {
        var configFilePath = Path.Combine("Database", "config.json");

        if (!File.Exists(configFilePath))
            throw new Exception($"The configuration file does not exist on path {configFilePath}.");

        var jsonString = File.ReadAllText(configFilePath);
        var sourceConfiguration = JsonSerializer.Deserialize<MySettings>(jsonString, JsonSerializerOptions.Web)!;

        var context = new DataSourceContext<MySettings>(
            ResourceLocator: new Uri("Database", UriKind.Relative),
            SourceConfiguration: sourceConfiguration,
            RequestConfiguration: default!
        );

        return context;
    }
}