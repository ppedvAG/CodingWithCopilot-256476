namespace LearnSpark.API.Models
{
	// Die Hauptklasse für den gesamten Lernpfad
	public class LearningPath
	{
		public string Id { get; set; } = Guid.NewGuid().ToString(); // Eindeutige ID
		public string Topic { get; set; } = string.Empty; // Das eingegebene Thema (z.B. "Python")
		public List<Module> Modules { get; set; } = new List<Module>(); // Liste der Module
	}

	// Ein Modul (z.B. "Grundlagen")
	public class Module
	{
		public string Title { get; set; } = string.Empty; // Titel des Moduls
		public string Description { get; set; } = string.Empty; // Kurze Beschreibung
		public List<SubTopic> SubTopics { get; set; } = new List<SubTopic>(); // Unterthemen
	}

	// Ein Unterthema (z.B. "Variablen und Datentypen")
	public class SubTopic
	{
		public string Title { get; set; } = string.Empty;
		public List<string> KeyConcepts { get; set; } = new List<string>(); // Lernziele/Konzepte
																			// Später können wir hier Ressourcen-Links hinzufügen 
	}

	// Eine Hilfsklasse für die Anfrage vom Frontend (User gibt nur das Thema ein)
	public class GenerateRequest
	{
		public string Topic { get; set; } = string.Empty;
	}
}