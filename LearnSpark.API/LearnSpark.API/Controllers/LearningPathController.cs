using LearnSpark.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace LearnSpark.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LearningPathController : ControllerBase
	{
		private readonly HttpClient _httpClient;

		// NEU: Unsere "In-Memory Datenbank" (Statische Liste)
		// Static bedeutet: Die Daten bleiben da, solange der Server läuft.
		private static List<LearningPath> _savedPaths = new List<LearningPath>();

		public LearningPathController()
		{
			_httpClient = new HttpClient();
			_httpClient.BaseAddress = new Uri("http://localhost:1234");
			// Timeout auf 10 Minuten (Deine Anpassung)
			_httpClient.Timeout = TimeSpan.FromMinutes(10);
		}

		// NEU: Endpunkt, um die Liste der gespeicherten Pfade abzurufen
		// Wird vom Frontend beim Start (ngOnInit) aufgerufen.
		[HttpGet]
		public IActionResult GetAllPaths()
		{
			// Wir drehen die Liste um, damit die neuesten ganz oben stehen
			return Ok(_savedPaths.OrderByDescending(p => p.Id));
		}

		[HttpPost("generate")]
		public async Task<IActionResult> GeneratePath([FromBody] GenerateRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Topic))
				return BadRequest("Bitte ein Thema angeben.");

			var systemPrompt = @"Du bist ein Backend-Prozess, der JSON Daten liefert.
ANTWORTE NUR MIT JSON.
Verwende KEINE Markdown-Formatierung.
Struktur:
{
  ""topic"": ""Thema"",
  ""modules"": [
    {
      ""title"": ""Titel"",
      ""description"": ""Beschreibung"",
      ""subTopics"": [
        {
          ""title"": ""Unterthema"",
          ""keyConcepts"": [""Konzept A""] 
        }
      ]
    }
  ]
}";

			// Dein Prompt für schnelle Tests (1 Modul)
			var userPrompt = $"Generiere einen kurzen Lernpfad für: '{request.Topic}'. Erstelle NUR 1 Modul mit 2 Unterthemen.";

			var aiRequest = new
			{
				model = "local-model",
				messages = new[]
				{
					new { role = "system", content = systemPrompt },
					new { role = "user", content = userPrompt }
				},
				temperature = 0.3,
				max_tokens = 1000
			};

			var jsonContent = new StringContent(JsonSerializer.Serialize(aiRequest), Encoding.UTF8, "application/json");

			try
			{
				var response = await _httpClient.PostAsync("/v1/chat/completions", jsonContent);
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();

				using var doc = JsonDocument.Parse(responseString);
				var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

				// Logging
				Console.WriteLine("--- KI ROH-ANTWORT START ---");
				Console.WriteLine(content);
				Console.WriteLine("--- KI ROH-ANTWORT ENDE ---");

				var cleanJson = CleanJsonString(content);

				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
				var learningPath = JsonSerializer.Deserialize<LearningPath>(cleanJson, options);

				if (learningPath != null)
				{
					learningPath.Id = Guid.NewGuid().ToString();
					learningPath.Topic = request.Topic;

					// NEU: Fortschritt initialisieren und in die Liste speichern
					learningPath.Progress = 0;
					_savedPaths.Add(learningPath);
				}

				return Ok(learningPath);
			}
			catch (JsonException jsonEx)
			{
				return StatusCode(500, $"JSON Parsing Fehler: {jsonEx.Message}. Schau in den Output für die Rohdaten.");
			}
			catch (Exception ex)
			{
				return StatusCode(500, $"Allgemeiner Fehler: {ex.Message}");
			}
		}

		private string CleanJsonString(string json)
		{
			if (string.IsNullOrEmpty(json)) return "";

			json = json.Replace("```json", "").Replace("```", "");

			int startIndex = json.IndexOf("{");
			int endIndex = json.LastIndexOf("}");

			if (startIndex >= 0 && endIndex > startIndex)
			{
				json = json.Substring(startIndex, endIndex - startIndex + 1);
			}

			return json.Trim();
		}
		// Füge das in deine Klasse LearningPathController ein:

		[HttpPost("toggle-progress")]
		public IActionResult ToggleProgress([FromBody] ToggleRequest request)
		{
			// 1. Pfad suchen
			var path = _savedPaths.FirstOrDefault(p => p.Id == request.PathId);
			if (path == null) return NotFound("Pfad nicht gefunden");

			// 2. Modul und Unterthema suchen (über Index, das ist am einfachsten)
			if (request.ModuleIndex >= 0 && request.ModuleIndex < path.Modules.Count)
			{
				var module = path.Modules[request.ModuleIndex];
				if (request.SubTopicIndex >= 0 && request.SubTopicIndex < module.SubTopics.Count)
				{
					// 3. Status umschalten (True <-> False)
					var subTopic = module.SubTopics[request.SubTopicIndex];
					subTopic.IsCompleted = !subTopic.IsCompleted;

					// 4. Gesamtfortschritt neu berechnen
					UpdateProgress(path);

					return Ok(path); // Gib den aktualisierten Pfad zurück
				}
			}
			return BadRequest("Index ungültig");
		}

		// Hilfsmethode zur Berechnung
		private void UpdateProgress(LearningPath path)
		{
			int totalItems = 0;
			int completedItems = 0;

			foreach (var mod in path.Modules)
			{
				foreach (var sub in mod.SubTopics)
				{
					totalItems++;
					if (sub.IsCompleted) completedItems++;
				}
			}

			if (totalItems > 0)
			{
				path.Progress = (int)((double)completedItems / totalItems * 100);
			}
			else
			{
				path.Progress = 0;
			}
		}
	}
}