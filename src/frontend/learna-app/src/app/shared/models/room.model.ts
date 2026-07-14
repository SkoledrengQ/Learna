export interface Room {
  id: number;
  name: string;
  building?: string | null;
  description?: string | null;
  capacity?: number | null;
}

export interface CreateRoomDto {
  name: string;
  building?: string | null;
  description?: string | null;
  capacity?: number | null;
}

export interface UpdateRoomDto {
  name: string;
  building?: string | null;
  description?: string | null;
  capacity?: number | null;
}
