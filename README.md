# Interpreter.Net
Auto-generate boilerplate code of interpreter pattern.

**Note**: This is currently just a proof of concept and not tested in production, so please use it with caution.

[![NuGet version](https://img.shields.io/nuget/v/Interpreter.Net.svg)](https://www.nuget.org/packages/Interpreter.Net/)

## Use
1. Add a new file that ends with `.Interpreter.cs`, e.g. `ReservationInstruction.Interpreter.cs`.
1. In there define a static class with all possible instructions, e.g.
    ```csharp
    using System;
    using System.Collections.Generic;

    namespace Ploeh.Samples.BookingApi
    {
        public static class ReservationInstruction
        {
            public static ReservationInstruction<bool> IsReservationInFuture(Reservation reservation)
                => new IsReservationInFuture(reservation);
            public static ReservationInstruction<IReadOnlyCollection<Reservation>> ReadReservations(DateTimeOffset date)
                => new ReadReservations(date);
            public static ReservationInstruction<int> CreateReservation(Reservation reservation)
                => new CreateReservation(reservation);
        }
    }
    ```
1. Build the project, e.g. run `dotnet build`. Note that the class name (`ReservationInstruction`) is used to define other types, e.g. for the above example we define `ReservationInstruction<TResult>`, `ReservationInstructionProgram`, `IReservationInstructionHandler` and more.
1. In another file, use the instructions to define a program, e.g.
    ```csharp
    public async ReservationInstructionProgram<int?> TryAccept(Reservation reservation)
    {
        if (!await ReservationInstruction.IsReservationInFuture(reservation))
            return null;

        var reservedSeats = (await ReservationInstruction.ReadReservations(reservation.Date))
                            .Sum(r => r.Quantity);
        if (Capacity < reservedSeats + reservation.Quantity)
            return null;

        reservation.IsAccepted = true;
        return await ReservationInstruction.CreateReservation(reservation);
    }
    ```
1. Define a handler that interprets your program, e.g.
    ```csharp
    using System.Collections.Generic;
    using System.Threading.Tasks;

    namespace Ploeh.Samples.BookingApi.UnitTests
    {
        public class StubReservationsHandler : IReservationInstructionHandler
        {
            private readonly bool isInFuture;
            private readonly IReadOnlyCollection<Reservation> reservations;
            private readonly int id;

            public StubReservationsHandler(
                bool isInFuture,
                IReadOnlyCollection<Reservation> reservations,
                int id)
            {
                this.isInFuture = isInFuture;
                this.reservations = reservations;
                this.id = id;
            }

            public Task<bool> Handle(IsReservationInFuture instruction)
            {
                return Task.FromResult(isInFuture);
            }

            public Task<IReadOnlyCollection<Reservation>> Handle(ReadReservations instruction)
            {
                return Task.FromResult(reservations);
            }

            public Task<int> Handle(CreateReservation instruction)
            {
                return Task.FromResult(id);
            }
        }
    }
    ```

    Because every method returns a `Task<T>` you can handle instructions asynchronously.

1. Use your program, e.g.
    ```csharp
    public async Task TryAcceptWithRealDatabase(Reservation reservation)
    {
        var connectionString = ...
        var handler = new SqlReservationsProgramHandler(connectionString);
        ReservationInstructionProgram<int?> program = TryAccept(reservation);
        int? id = await program.Run(handler);
    }
    ```

For a full example check out https://github.com/johannesegger/dependency-injection-revisited/tree/master/CSharp.

## Acknowledgments
There is no way I could have created this library if there wasn't [Mark Seemann](https://twitter.com/ploeh) with his [great article about an alternative for dependency injection](http://blog.ploeh.dk/2018/07/24/dependency-injection-revisited/) and [Nick Palladinos](https://twitter.com/NickPalladinos) for his great [Effects library for C#](https://github.com/nessos/Eff) and some help on understanding it. The code generation via an MSBuild target is stolen from [protobuf-net](https://github.com/mgravell/protobuf-net), I accidentally saw that on [Twitter](https://twitter.com/marcgravell/status/1025139557619122181).
