using Model.Entities;

namespace Model.DTOs;

public record SuitableRoom(OperationRoom Room, DateTime? AvailableAt);
