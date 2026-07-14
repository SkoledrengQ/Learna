import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { RoomService } from '../../../services/room.service';
import { Room } from '../../../../../shared/models/room.model';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-room-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatDialogModule,
    MatSnackBarModule,
    MatTooltipModule,
    TranslocoModule
  ],
  templateUrl: './room-list.component.html',
  styleUrl: './room-list.component.scss'
})
export class RoomListComponent implements OnInit {
  private roomService = inject(RoomService);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  rooms = signal<Room[]>([]);
  displayedColumns: string[] = ['name', 'building', 'capacity', 'actions'];
  isLoading = signal(true);

  ngOnInit(): void {
    this.loadRooms();
  }

  loadRooms(): void {
    this.isLoading.set(true);
    this.roomService.getRooms().subscribe({
      next: (rooms) => {
        this.rooms.set(rooms);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading rooms:', error);
        this.snackBar.open(this.transloco.translate('admin.rooms.loadFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onAddRoom(): void {
    this.router.navigate(['/admin/rooms/new']);
  }

  onEditRoom(id: number): void {
    this.router.navigate(['/admin/rooms', id]);
  }

  onDeleteRoom(room: Room): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: this.transloco.translate('admin.rooms.deleteTitle'),
        message: this.transloco.translate('admin.rooms.deleteMessage', { name: room.name })
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.roomService.deleteRoom(room.id).subscribe({
          next: () => {
            this.snackBar.open(this.transloco.translate('admin.rooms.deleteSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
            this.loadRooms();
          },
          error: (error) => {
            console.error('Error deleting room:', error);
            this.snackBar.open(this.transloco.translate('admin.rooms.deleteFailed'), this.transloco.translate('common.close'), { duration: 3000 });
          }
        });
      }
    });
  }
}
