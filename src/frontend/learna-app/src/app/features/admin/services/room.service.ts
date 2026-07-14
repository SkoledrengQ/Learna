import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { Room, CreateRoomDto, UpdateRoomDto } from '../../../shared/models/room.model';

@Injectable({
  providedIn: 'root'
})
export class RoomService {
  private apiService = inject(ApiService);
  private readonly basePath = 'rooms';

  getRooms(): Observable<Room[]> {
    return this.apiService.get<Room[]>(this.basePath);
  }

  getRoom(id: number): Observable<Room> {
    return this.apiService.get<Room>(`${this.basePath}/${id}`);
  }

  createRoom(dto: CreateRoomDto): Observable<Room> {
    return this.apiService.post<Room>(this.basePath, dto);
  }

  updateRoom(id: number, dto: UpdateRoomDto): Observable<Room> {
    return this.apiService.put<Room>(`${this.basePath}/${id}`, dto);
  }

  deleteRoom(id: number): Observable<void> {
    return this.apiService.delete<void>(`${this.basePath}/${id}`);
  }
}
