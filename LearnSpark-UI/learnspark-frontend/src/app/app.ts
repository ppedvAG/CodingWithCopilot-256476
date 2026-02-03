import { Component, ChangeDetectorRef } from '@angular/core'; // <--- 1. Import hinzugefügt
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
export class App {
  title = 'LearnSpark';

  topic: string = '';
  isLoading: boolean = false;
  learningPath: any = null;
  errorMessage: string = '';

  // 2. ChangeDetectorRef hier im Constructor hinzufügen
  constructor(private http: HttpClient, private cd: ChangeDetectorRef) { }

  generatePath() {
    if (!this.topic.trim()) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.learningPath = null;

    const apiUrl = 'https://localhost:7066/api/LearningPath/generate';

    this.http.post(apiUrl, { topic: this.topic }).subscribe({
      next: (response) => {
        console.log('Erfolg!', response);
        this.learningPath = response;
        this.isLoading = false;

        // 3. Angular zwingen, die Oberfläche zu aktualisieren!
        this.cd.detectChanges();
      },
      error: (error) => {
        console.error('Fehler:', error);
        this.errorMessage = 'Fehler beim Generieren. Läuft das Backend?';
        this.isLoading = false;

        // Auch im Fehlerfall aktualisieren
        this.cd.detectChanges();
      }
    });
  }
}