import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { FileResource } from '../../shared/models/file-resource.model';

@Injectable({ providedIn: 'root' })
export class MaterialsService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  listSubjectGroup(id: number): Observable<FileResource[]> { return this.http.get<FileResource[]>(`${this.api}/subject-groups/${id}/files`); }
  listLesson(id: number): Observable<FileResource[]> { return this.http.get<FileResource[]>(`${this.api}/lessons/${id}/files`); }
  listMine(): Observable<FileResource[]> { return this.http.get<FileResource[]>(`${this.api}/files/my`); }
  uploadSubjectGroup(id: number, file: File, description: string): Observable<FileResource> { return this.upload(`subject-groups/${id}/files`, file, description); }
  uploadLesson(id: number, file: File, description: string): Observable<FileResource> { return this.upload(`lessons/${id}/files`, file, description); }
  delete(id: number): Observable<void> { return this.http.delete<void>(`${this.api}/files/${id}`); }

  download(file: FileResource): void {
    this.http.get(`${this.api}/files/${file.id}/download`, { responseType: 'blob' }).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = file.originalFileName;
      anchor.click();
      URL.revokeObjectURL(url);
    });
  }

  private upload(path: string, file: File, description: string): Observable<FileResource> {
    const data = new FormData();
    data.append('file', file, file.name);
    if (description.trim()) data.append('description', description.trim());
    return this.http.post<FileResource>(`${this.api}/${path}`, data);
  }
}
