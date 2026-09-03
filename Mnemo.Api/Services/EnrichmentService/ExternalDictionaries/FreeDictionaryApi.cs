using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Mnemo.Contracts.Entry;
using Mnemo.Shared;
using Mnemo.Shared.Enums;
using System.Text.Json;

namespace Mnemo.Services.EnrichmentService.ExternalDictionaries
{
    public class FreeDictionaryApi : IExternalDictionary
    {
        private readonly ILogger<IExternalDictionary> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;


        public FreeDictionaryApi(ILogger<IExternalDictionary> logger, HttpClient httpClient, IMemoryCache cache)
        {
            _logger = logger;
            _httpClient = httpClient;
            _cache = cache;
        }


        private static readonly TimeSpan _cacheLifetime = TimeSpan.FromHours(12);

        public async Task<RequestResult<EnrichResponse?>> GetEnrichAsync(string foreign, PartOfSpeech? partOfSpeech)
        {
            _logger.LogInformation("Attempting to get EnrichResponse (Foreign:{Foreign} - PartOfSpeech:{PartOfSpeech})", foreign, partOfSpeech);

            if (string.IsNullOrWhiteSpace(foreign) || !partOfSpeech.HasValue)
            {
                _logger.LogWarning("Foreign word is required");
                return RequestResult<EnrichResponse?>.Failure(ErrorCode.InvalidData);
            }

            // Try get from cache
            var cacheKey = $"{foreign.ToLowerInvariant()} - {partOfSpeech.Value.ToString().ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out EnrichResponse? cached))
            {
                _logger.LogInformation("Return EnrichResponse (isNull:{isNull}) from cache (Foreign:{Foreign} - PartOfSpeech:{PartOfSpeech})", cached == null, foreign, partOfSpeech);
                return RequestResult<EnrichResponse?>.Success(cached);
            }


            try
            {
                var result = await _httpClient.GetAsync($"https://api.dictionaryapi.dev/api/v2/entries/en/{foreign}");

                if (!result.IsSuccessStatusCode)
                {
                    _logger.LogInformation("API returned StatusCode:{StatusCode} (Foreign:{Foreign} - PartOfSpeech:{PartOfSpeech})", result.StatusCode, foreign, partOfSpeech);

                    // Cache status 404 as null
                    if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
                        _cache.Set(cacheKey, (EnrichResponse?)null, _cacheLifetime);

                    return RequestResult<EnrichResponse?>.Success(null);
                }


                var entries = await result.Content.ReadFromJsonAsync<List<FreeDictionaryEntry>>();

                if (entries == null || !entries.Any())
                    throw new JsonException("JSON is null or empty");


                var entry = entries[0];

                Phonetic? phonetic = SelectBestPhonetic(entry.Phonetics);
                Meaning? meaning = SelectMeaning(entry.Meanings, partOfSpeech.Value);

                var enrichResponse = new EnrichResponse()
                {
                    Transcription = phonetic?.Text,
                    AudioUrl = phonetic?.Audio,
                    Synonyms = meaning?.Synonyms?.ToArray(),
                    Antonyms = meaning?.Antonyms?.ToArray(),
                };

                _cache.Set(cacheKey, enrichResponse, _cacheLifetime);
                _logger.LogInformation("Successfully extracted EnrichResponse (Foreign:{Foreign} - PartOfSpeech:{PartOfSpeech})", foreign, partOfSpeech);

                return RequestResult<EnrichResponse?>.Success(enrichResponse);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning("Timeout fetching (Foreign:{Foreign} - PartOfSpeech:{PartOfSpeech}): {Message}", foreign, partOfSpeech, ex.Message);
                return RequestResult<EnrichResponse?>.Failure(ErrorCode.ExternalDictionaryError, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Network error (Foreign:{Foreign} - PartOfSpeech:{PartOfSpeech}): {Message}", foreign, partOfSpeech, ex.Message);
                return RequestResult<EnrichResponse?>.Failure(ErrorCode.ExternalDictionaryError, ex.Message);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error (Foreign:{Foreign} - PartOfSpeech:{PartOfSpeech})", foreign, partOfSpeech);
                return RequestResult<EnrichResponse?>.Failure(ErrorCode.ExternalDictionaryError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error (Foreign:{Foreign} - PartOfSpeech:{PartOfSpeech})", foreign, partOfSpeech);
                return RequestResult<EnrichResponse?>.Failure(ErrorCode.ExternalDictionaryError, ex.Message);
            }
        }


        private static readonly string[] _audioPriority = { "-uk", "-us", "-au" };

        private Phonetic? SelectBestPhonetic(List<Phonetic>? phonetics)
        {
            if (phonetics == null || !phonetics.Any())
                return null;


            var withAudio = phonetics.Where(p => !string.IsNullOrWhiteSpace(p.Audio)).ToList();

            if (!withAudio.Any())
                return phonetics.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text));


            foreach (var prioritySuffix in _audioPriority)
            {
                var withTextAndAudio = withAudio
                    .FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text) && p.Audio!.Contains(prioritySuffix, StringComparison.OrdinalIgnoreCase));

                if (withTextAndAudio != null)
                    return withTextAndAudio;
            }

            return phonetics.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text));
        }

        private Meaning? SelectMeaning(List<Meaning>? meanings, PartOfSpeech partOfSpeech)
        {
            if (meanings == null || !meanings.Any())
                return null;

            return meanings.FirstOrDefault(m => string.Equals(m.PartOfSpeech, partOfSpeech.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        private class FreeDictionaryEntry
        {
            public string? Word { get; set; }
            public string? Phonetic { get; set; }
            public List<Phonetic>? Phonetics { get; set; }
            public List<Meaning>? Meanings { get; set; }
        }

        private class Phonetic
        {
            public string? Text { get; set; }
            public string? Audio { get; set; }
        }

        private class Meaning
        {
            public string? PartOfSpeech { get; set; }
            public List<string>? Synonyms { get; set; }
            public List<string>? Antonyms { get; set; }
        }
    }
}
