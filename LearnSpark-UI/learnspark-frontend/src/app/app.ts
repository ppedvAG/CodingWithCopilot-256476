import { Component, ChangeDetectorRef, OnInit } from '@angular/core'; // OnInit importieren
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit { // "implements OnInit" hinzufügen
  title = 'LearnSpark';

  topic: string = '';
  isLoading: boolean = false;
  learningPath: any = null;
  errorMessage: string = '';

  // NEU: Liste für die Sidebar
  savedPaths: any[] = [];

  constructor(private http: HttpClient, private cd: ChangeDetectorRef) { }

  // Wird automatisch ausgeführt, wenn die Seite lädt
  ngOnInit() {
    this.loadSavedPaths();
  }

  // Pfade vom Backend laden
  loadSavedPaths() {
    this.http.get<any[]>('https://localhost:7066/api/LearningPath').subscribe({
      next: (paths) => {
        this.savedPaths = paths;
        // Falls wir noch keinen Pfad offen haben, zeigen wir aber noch nichts an
      }
    });
  }

  // Wenn man in der Sidebar auf einen Pfad klickt
  selectPath(path: any) {
    this.learningPath = path;
  }

  generatePath() {
    if (!this.topic.trim()) return;

    this.isLoading = true;
    this.errorMessage = '';

    // URL anpassen falls nötig
    const apiUrl = 'https://localhost:7066/api/LearningPath/generate';

    this.http.post(apiUrl, { topic: this.topic }).subscribe({
      next: (response) => {
        console.log('Erfolg!', response);
        this.learningPath = response;
        this.isLoading = false;

        // --- WICHTIG: HIER IST DIE ÄNDERUNG ---
        // Wir laden die Liste neu, damit der neue Pfad sofort links erscheint
        this.loadSavedPaths();
        // ---------------------------------------

        this.cd.detectChanges();
      },
      error: (error) => {
        console.error('Fehler:', error);
        this.errorMessage = 'Fehler beim Generieren.';
        this.isLoading = false;
        this.cd.detectChanges();
      }
    });
  }
  toggleSubTopic(moduleIndex: number, subTopicIndex: number) {
    if (!this.learningPath) return;

    const request = {
      pathId: this.learningPath.id,
      moduleIndex: moduleIndex,
      subTopicIndex: subTopicIndex
    };

    // Optimistische UI: Wir ändern es sofort lokal, damit es sich schnell anfühlt
    const sub = this.learningPath.modules[moduleIndex].subTopics[subTopicIndex];
    sub.isCompleted = !sub.isCompleted; // Lokal umschalten

    // API Aufruf
    this.http.post<any>('https://localhost:7066/api/LearningPath/toggle-progress', request)
      .subscribe({
        next: (updatedPath) => {
          // Wir übernehmen den neu berechneten Fortschritt vom Backend
          this.learningPath.progress = updatedPath.progress;

          // Auch in der Sidebar aktualisieren wir den passenden Eintrag
          const sidebarItem = this.savedPaths.find(p => p.id === updatedPath.id);
          if (sidebarItem) {
            sidebarItem.progress = updatedPath.progress;
          }

          this.cd.detectChanges();
        },
        error: (err) => console.error(err)
      });
  }


}