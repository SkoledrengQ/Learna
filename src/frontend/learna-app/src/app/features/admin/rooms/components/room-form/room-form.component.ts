import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { RoomService } from '../../../services/room.service';
import { CreateRoomDto, UpdateRoomDto } from '../../../../../shared/models/room.model';
import { PageHeaderComponent } from '../../../../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-room-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule,
    TranslocoModule,
    PageHeaderComponent
  ],
  templateUrl: './room-form.component.html',
  styleUrl: './room-form.component.scss'
})
export class RoomFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private roomService = inject(RoomService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private snackBar = inject(MatSnackBar);
  private transloco = inject(TranslocoService);

  roomForm!: FormGroup;
  isEditMode = false;
  roomId?: number;
  isLoading = signal(false);

  ngOnInit(): void {
    this.initializeForm();
    this.checkEditMode();
  }

  private initializeForm(): void {
    this.roomForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      building: ['', Validators.maxLength(200)],
      description: ['', Validators.maxLength(1000)],
      capacity: [null]
    });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.roomId = +id;
      this.loadRoom(this.roomId);
    }
  }

  private loadRoom(id: number): void {
    this.isLoading.set(true);
    this.roomService.getRoom(id).subscribe({
      next: (room) => {
        this.roomForm.patchValue({
          name: room.name,
          building: room.building,
          description: room.description,
          capacity: room.capacity
        });
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading room:', error);
        this.snackBar.open(this.transloco.translate('admin.rooms.loadFailedSingle'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
        this.router.navigate(['/admin/rooms']);
      }
    });
  }

  onSubmit(): void {
    if (this.roomForm.invalid) {
      this.roomForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);

    if (this.isEditMode && this.roomId) {
      this.updateRoom();
    } else {
      this.createRoom();
    }
  }

  private buildDto(): CreateRoomDto | UpdateRoomDto {
    const v = this.roomForm.value;
    return {
      name: v.name,
      building: v.building || null,
      description: v.description || null,
      capacity: v.capacity || null
    };
  }

  private createRoom(): void {
    this.roomService.createRoom(this.buildDto()).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate('admin.rooms.createSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/rooms']);
      },
      error: (error) => {
        console.error('Error creating room:', error);
        this.snackBar.open(this.transloco.translate('admin.rooms.createFailed'), this.transloco.translate('common.close'), { duration: 5000 });
        this.isLoading.set(false);
      }
    });
  }

  private updateRoom(): void {
    this.roomService.updateRoom(this.roomId!, this.buildDto()).subscribe({
      next: () => {
        this.snackBar.open(this.transloco.translate('admin.rooms.updateSuccess'), this.transloco.translate('common.close'), { duration: 3000 });
        this.router.navigate(['/admin/rooms']);
      },
      error: (error) => {
        console.error('Error updating room:', error);
        this.snackBar.open(this.transloco.translate('admin.rooms.updateFailed'), this.transloco.translate('common.close'), { duration: 3000 });
        this.isLoading.set(false);
      }
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/rooms']);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.roomForm.get(fieldName);
    if (field?.hasError('required')) {
      return this.transloco.translate('validation.required');
    }
    if (field?.hasError('maxlength')) {
      return this.transloco.translate('validation.maxLength');
    }
    return '';
  }
}
