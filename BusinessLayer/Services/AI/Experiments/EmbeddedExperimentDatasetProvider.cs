using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using System.Reflection;
using System.Text.Json;

namespace Business.Services.AI.Experiments;

public sealed class EmbeddedExperimentDatasetProvider(IUnitOfWork unitOfWork) : IExperimentDatasetProvider
{
    private const string ResourceName = "Business.Services.AI.Experiments.Data.db201-vi-50.json";
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<TestDatasetDto> GetDatasetAsync(CancellationToken cxlTkn = default)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded experiment dataset '{ResourceName}' was not found.");
        var dataset = await JsonSerializer.DeserializeAsync<TestDatasetDto>(stream, JsonOptions, cxlTkn)
            ?? throw new InvalidOperationException("The embedded experiment dataset is empty or malformed.");
        Validate(dataset);
        return dataset;
    }

    public async Task<int> ImportAsync(CancellationToken cxlTkn = default)
    {
        var dataset = await GetDatasetAsync(cxlTkn);
        var subject = (await _unitOfWork.Subjects.GetAsync(filter: e => e.Code == dataset.SubjectCode, asNoTracking: true, cancellationToken: cxlTkn)).SingleOrDefault()
            ?? throw new EntityNotFoundException($"The experiment subject '{dataset.SubjectCode}' does not exist.");

        var existing = (await _unitOfWork.TestQuestions.GetAsync(filter: e => e.SubjectId == subject.Id, cancellationToken: cxlTkn)).ToDictionary(e => e.ExternalId, StringComparer.Ordinal);
        var changed = 0;

        foreach (var source in dataset.Questions)
        {
            if (!existing.TryGetValue(source.ExternalId, out var target))
            {
                _unitOfWork.TestQuestions.Insert(new TestQuestion
                {
                    SubjectId = subject.Id,
                    ExternalId = source.ExternalId,
                    Language = dataset.Language,
                    Question = source.Question,
                    GroundTruth = source.GroundTruth
                });
                changed++;
                continue;
            }

            if (target.Language == dataset.Language && target.Question == source.Question && target.GroundTruth == source.GroundTruth) continue;

            target.Language = dataset.Language;
            target.Question = source.Question;
            target.GroundTruth = source.GroundTruth;
            _unitOfWork.TestQuestions.Update(target);
            changed++;
        }

        if (changed > 0) await _unitOfWork.SaveAsync(cxlTkn);
        return changed;
    }

    private static void Validate(TestDatasetDto dataset)
    {
        if (dataset.DatasetKey != "db201-vi-50-v1") throw new EntityValidationException("The experiment dataset key must be 'db201-vi-50-v1'.", nameof(dataset.DatasetKey));
        if (dataset.Language != "vi") throw new EntityValidationException("The DB201 experiment dataset language must be Vietnamese.", nameof(dataset.Language));
        if (dataset.SubjectCode != "DB201") throw new EntityValidationException("The experiment dataset subject must be DB201.", nameof(dataset.SubjectCode));
        if (dataset.Questions.Count != 50) throw new EntityValidationException("The DB201 experiment dataset must contain exactly 50 questions.", nameof(dataset.Questions));

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < dataset.Questions.Count; i++)
        {
            var question = dataset.Questions[i];
            var expectedId = $"DB201-VI-{i + 1:D3}";
            if (question.ExternalId != expectedId) throw new EntityValidationException($"Expected question ID '{expectedId}', found '{question.ExternalId}'.", nameof(question.ExternalId));
            if (!ids.Add(question.ExternalId)) throw new EntityValidationException($"Duplicate question ID '{question.ExternalId}'.", nameof(question.ExternalId));
            if (string.IsNullOrWhiteSpace(question.Question)) throw new EntityValidationException($"Question '{question.ExternalId}' has no question text.", nameof(question.Question));
            if (string.IsNullOrWhiteSpace(question.GroundTruth)) throw new EntityValidationException($"Question '{question.ExternalId}' has no ground truth.", nameof(question.GroundTruth));
        }
    }
}
