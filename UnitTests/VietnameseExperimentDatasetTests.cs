using Domain.Contracts.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UnitTests;

[TestFixture]
public class VietnameseExperimentDatasetTests
{
    private string _datasetPath = null!;

    [SetUp]
    public void SetUp()
    {
        // Resolve path to the json file
        var baseDir = TestContext.CurrentContext.TestDirectory;
        // Search up for the solution folder if running from build artifact directory
        var solutionDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "BusinessLayer", "Services", "AI", "Experiments", "Data"));

        _datasetPath = Path.Combine(solutionDir, "db201-vi-50.json");

        if (!File.Exists(_datasetPath))
        {
            // Fallback for visual studio execution
            var altPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "BusinessLayer", "Services", "AI", "Experiments", "Data", "db201-vi-50.json"));
            if (File.Exists(altPath))
            {
                _datasetPath = altPath;
            }
        }
    }

    [Test]
    public void DatasetFile_ExistsAndIsParsable()
    {
        // Assert
        Assert.That(File.Exists(_datasetPath), Is.True, $"Dataset file does not exist at path: {_datasetPath}");

        var json = File.ReadAllText(_datasetPath);
        var dataset = JsonSerializer.Deserialize<TestDatasetDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(dataset, Is.Not.Null);
        Assert.That(dataset!.DatasetKey, Is.EqualTo("db201-vi-50-v1"));
        Assert.That(dataset.Language, Is.EqualTo("vi"));
        Assert.That(dataset.SubjectCode, Is.EqualTo("DB201"));
        Assert.That(dataset.Questions, Is.Not.Null);
        Assert.That(dataset.Questions.Count, Is.EqualTo(50));

        var idPattern = new Regex(@"^DB201-VI-(0[0-9]{2})$");

        for (int i = 0; i < 50; i++)
        {
            var q = dataset.Questions[i];
            int expectedNum = i + 1;
            string expectedId = $"DB201-VI-{expectedNum:D3}";

            Assert.That(q.ExternalId, Is.EqualTo(expectedId));
            Assert.That(q.Question, Is.Not.Null.And.Not.Empty);
            Assert.That(q.GroundTruth, Is.Not.Null.And.Not.Empty);

            // Assert that it contains Vietnamese diacritics/characters
            bool containsVietnamese = Regex.IsMatch(q.Question, "[àáảãạâầấẩẫậăằắẳẵặèéẻẽẹêềếểễệđìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ]") ||
                                     Regex.IsMatch(q.GroundTruth, "[àáảãạâầấẩẫậăằắẳẵặèéẻẽẹêềếểễệđìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵ]");
            Assert.That(containsVietnamese, Is.True, $"Question {q.ExternalId} or its GroundTruth does not seem to contain Vietnamese characters.");
        }
    }
}
