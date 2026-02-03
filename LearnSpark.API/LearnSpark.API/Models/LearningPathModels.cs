namespace LearnSpark.API.Models
{
	// Die Hauptklasse für den gesamten Lernpfad
	public class LearningPath
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public string Topic { get; set; } = string.Empty;
		public int Progress { get; set; } = 0; // <--- NEU: Fortschritt in Prozent
		public List<Module> Modules { get; set; } = new List<Module>();
	}
	

	// Ein Modul (z.B. "Grundlagen")
	public class Module
	{
		public string Title { get; set; } = string.Empty; // Titel des Moduls
		public string Description { get; set; } = string.Empty; // Kurze Beschreibung
		public List<SubTopic> SubTopics { get; set; } = new List<SubTopic>(); // Unterthemen
	}

	public class SubTopic
	{
		public string Title { get; set; } = string.Empty;
		public List<string> KeyConcepts { get; set; } = new List<string>();

		// NEU: Damit wir wissen, ob es abgehakt ist
		public bool IsCompleted { get; set; } = false;
	}

	// NEU: Ein kleines Hilfsobjekt für die Anfrage vom Frontend
	public class ToggleRequest
	{
		public string PathId { get; set; }
		public int ModuleIndex { get; set; }
		public int SubTopicIndex { get; set; }
	}

	// Eine Hilfsklasse für die Anfrage vom Frontend (User gibt nur das Thema ein)
	public class GenerateRequest
	{
		public string Topic { get; set; } = string.Empty;
	}

	
}