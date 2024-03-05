using System.Collections.Concurrent;
using Model.Entities;

namespace Model;

public static class Data
{
    public static ConcurrentQueue<ReservationRequest> Queue { get; } = new();
    public static IEnumerable<Doctor> Doctors { get; } = new[]
    {
        new Doctor
        {
            Id = 1,
            Type = DoctorType.HeartSurgeon
        },
        new Doctor
        {
            Id = 2,
            Type = DoctorType.HeartSurgeon
        },
        new Doctor
        {
            Id = 3,
            Type = DoctorType.HeartSurgeon
        },
        new Doctor
        {
            Id = 4,
            Type = DoctorType.BrainSurgeon
        },
        new Doctor
        {
            Id = 5,
            Type = DoctorType.BrainSurgeon
        },
        new Doctor
        {
            Id = 6,
            Type = DoctorType.BrainSurgeon
        }
    };

    public static IEnumerable<Hospital> Hospitals { get; } = new[]
    {
        new Hospital
        {
            Id = 1,
            Rooms = new List<OperationRoom>
            {
                new OperationRoom
                {
                    Id = 1,
                    WithMRI = true,
                    WithCT = true,
                    WithECG = true
                },
                new OperationRoom
                {
                    Id = 2,
                    WithMRI = true,
                    WithCT = true,
                },
                new OperationRoom
                {
                    Id = 3,
                    WithMRI = true,
                    WithCT = true,
                },
                new OperationRoom
                {
                    Id = 4,
                    WithMRI = true,
                    WithECG = true,
                },
                new OperationRoom
                {
                    Id = 5,
                    WithMRI = true,
                    WithECG = true
                }
            }
        }
    };
}
