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

		public LearningPathController()
		{
			_httpClient = new HttpClient();
			_httpClient.BaseAddress = new Uri("http://localhost:1234");
			// ÄNDERUNG: Timeout auf 10 Minuten hochsetzen
			_httpClient.Timeout = TimeSpan.FromMinutes(10);
		}

		[HttpPost("generate")]
		public async Task<IActionResult> GeneratePath([FromBody] GenerateRequest request)
		{
			// ... (Validierung bleibt gleich) ...

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

			// ÄNDERUNG: Wir bitten für den Test nur um EIN Modul, damit es schneller geht.
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
				max_tokens = 500 // Begrenzt die Antwortlänge, damit er sich nicht totläuft
			};

			// ... (Rest der Methode bleibt gleich inkl. CleanJsonString) ...

			var jsonContent = new StringContent(JsonSerializer.Serialize(aiRequest), Encoding.UTF8, "application/json");

			try
			{
				var response = await _httpClient.PostAsync("/v1/chat/completions", jsonContent);
				response.EnsureSuccessStatusCode();

				var responseString = await response.Content.ReadAsStringAsync();

				using var doc = JsonDocument.Parse(responseString);
				var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

				// LOGGING: Schau im Visual Studio "Output" Fenster nach dieser Zeile, wenn es knallt!
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
				}

				return Ok(learningPath);
			}
			catch (JsonException jsonEx)
			{
				// Gibt dir genauere Infos zurück, anstatt nur "500"
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

			// 1. Markdown Code Blöcke entfernen
			json = json.Replace("```json", "").Replace("```", "");

			// 2. Versuchen, den Start und das Ende des JSON-Objekts zu finden
			int startIndex = json.IndexOf("{");
			int endIndex = json.LastIndexOf("}");

			if (startIndex >= 0 && endIndex > startIndex)
			{
				json = json.Substring(startIndex, endIndex - startIndex + 1);
			}

			return json.Trim();
		}
	}
}