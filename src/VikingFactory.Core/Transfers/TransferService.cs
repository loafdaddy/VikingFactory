using System;
using System.Collections.Generic;
using System.Linq;
using VikingFactory.Core.Items;

namespace VikingFactory.Core.Transfers
{
    public enum TransferState
    {
        Planned,
        Reserved,
        Escrowed,
        Delivered,
        Committed,
        RecoveryRequired
    }

    public sealed class Transfer
    {
        public Guid Id { get; set; }
        public string SourceName { get; set; } = "";
        public string DestinationName { get; set; } = "";
        public string PrefabId { get; set; } = "";
        public int Count { get; set; }
        public int ReservationId { get; set; }
        public TransferState State { get; set; }
        public string Reason { get; set; } = "";
        public ItemRecord? Escrow { get; set; }
    }

    /// <summary>
    /// Chest-to-buffer transfer. An unknown delivery stays in escrow. It is not refunded and resent.
    /// </summary>
    public sealed class TransferService
    {
        private readonly Dictionary<string, ItemBuffer> _buffers = new Dictionary<string, ItemBuffer>();
        private readonly Dictionary<Guid, Transfer> _transfers = new Dictionary<Guid, Transfer>();

        public void Register(ItemBuffer buffer)
        {
            _buffers[buffer.Name] = buffer;
        }

        public ItemBuffer GetBuffer(string name)
        {
            return _buffers[name];
        }

        public Transfer Get(Guid id)
        {
            return _transfers[id];
        }

        public IEnumerable<Transfer> All()
        {
            return _transfers.Values;
        }

        public int EscrowCount()
        {
            return _transfers.Values.Where(t => t.Escrow != null).Sum(t => t.Escrow!.Count);
        }

        public Transfer Plan(string source, string destination, string prefabId, int count)
        {
            Require(source);
            Require(destination);
            var transfer = new Transfer
            {
                Id = Guid.NewGuid(),
                SourceName = source,
                DestinationName = destination,
                PrefabId = prefabId,
                Count = count,
                State = TransferState.Planned,
                Reason = "Planned"
            };
            _transfers[transfer.Id] = transfer;
            return transfer;
        }

        public void Reserve(Guid id)
        {
            var transfer = _transfers[id];
            if (transfer.State != TransferState.Planned)
                return;

            var source = _buffers[transfer.SourceName];
            var destination = _buffers[transfer.DestinationName];
            var probe = new ItemRecord(transfer.PrefabId, transfer.Count);
            if (!destination.CanAccept(probe))
            {
                transfer.Reason = "Output full";
                return;
            }

            if (!source.TryReserve(transfer.PrefabId, transfer.Count, out var reservationId))
            {
                transfer.Reason = "Missing input";
                return;
            }

            transfer.ReservationId = reservationId;
            transfer.State = TransferState.Reserved;
            transfer.Reason = "Reserved";
        }

        public void Escrow(Guid id)
        {
            var transfer = _transfers[id];
            if (transfer.State != TransferState.Reserved)
                return;

            var source = _buffers[transfer.SourceName];
            var detached = source.DetachReserved(transfer.ReservationId, transfer.PrefabId, transfer.Count);
            if (detached == null)
            {
                source.ReleaseReservation(transfer.ReservationId);
                transfer.State = TransferState.RecoveryRequired;
                transfer.Reason = "Source lost the reserved item";
                return;
            }

            transfer.Escrow = detached;
            transfer.State = TransferState.Escrowed;
            transfer.Reason = "Escrowed";
        }

        public void Deliver(Guid id)
        {
            var transfer = _transfers[id];
            if (transfer.State != TransferState.Escrowed || transfer.Escrow == null)
                return;

            var destination = _buffers[transfer.DestinationName];
            if (!destination.CanAccept(transfer.Escrow))
            {
                transfer.State = TransferState.RecoveryRequired;
                transfer.Reason = "Destination filled after escrow. Item is held, not resent.";
                return;
            }

            destination.Add(transfer.Escrow);
            transfer.State = TransferState.Delivered;
            transfer.Reason = "Delivered";
        }

        public void Commit(Guid id)
        {
            var transfer = _transfers[id];
            if (transfer.State != TransferState.Delivered)
                return;

            transfer.Escrow = null;
            transfer.State = TransferState.Committed;
            transfer.Reason = "Committed";
        }

        public bool TryRun(Guid id)
        {
            Reserve(id);
            Escrow(id);
            Deliver(id);
            Commit(id);
            return _transfers[id].State == TransferState.Committed;
        }

        public IReadOnlyList<ItemRecord> TakeRecovery(Guid id, ItemBuffer recovery)
        {
            var transfer = _transfers[id];
            if (transfer.State != TransferState.RecoveryRequired || transfer.Escrow == null)
                return Array.Empty<ItemRecord>();

            var item = transfer.Escrow;
            recovery.Add(item);
            transfer.Escrow = null;
            transfer.Reason = "Recovered once";
            return new[] { item };
        }

        public WorkshopSnapshot Export()
        {
            return new WorkshopSnapshot
            {
                Buffers = _buffers.Values.Select(b => new BufferSnapshot
                {
                    Name = b.Name,
                    SlotCount = b.SlotCount,
                    Items = b.Snapshot().ToList()
                }).ToList(),
                Transfers = _transfers.Values.Select(Clone).ToList()
            };
        }

        public static TransferService Import(WorkshopSnapshot snapshot)
        {
            var service = new TransferService();
            foreach (var buffer in snapshot.Buffers)
            {
                var live = new ItemBuffer(buffer.Name, buffer.SlotCount);
                live.RestoreFrom(buffer.Items);
                service.Register(live);
            }

            foreach (var transfer in snapshot.Transfers)
                service._transfers[transfer.Id] = Clone(transfer);

            return service;
        }

        public int WorldItemCount()
        {
            return _buffers.Values.Sum(b => b.TotalItems()) + EscrowCount();
        }

        private void Require(string name)
        {
            if (!_buffers.ContainsKey(name))
                throw new InvalidOperationException("Unknown buffer '" + name + "'.");
        }

        private static Transfer Clone(Transfer transfer)
        {
            return new Transfer
            {
                Id = transfer.Id,
                SourceName = transfer.SourceName,
                DestinationName = transfer.DestinationName,
                PrefabId = transfer.PrefabId,
                Count = transfer.Count,
                ReservationId = transfer.ReservationId,
                State = transfer.State,
                Reason = transfer.Reason,
                Escrow = transfer.Escrow
            };
        }
    }

    public sealed class WorkshopSnapshot
    {
        public List<BufferSnapshot> Buffers { get; set; } = new List<BufferSnapshot>();
        public List<Transfer> Transfers { get; set; } = new List<Transfer>();
    }

    public sealed class BufferSnapshot
    {
        public string Name { get; set; } = "";
        public int SlotCount { get; set; }
        public List<ItemRecord> Items { get; set; } = new List<ItemRecord>();
    }
}
