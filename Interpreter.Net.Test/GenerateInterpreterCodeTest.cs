using System;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Interpreter.Net.Test
{
    public class GenerateInterpreterCodeTest
    {
        [Fact]
        public void ShouldExtractInfoFromSyntaxTree()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"using System;
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
");
            var info = GenerateInterpreterCode.ExtractInterpreterInfo(syntaxTree);
            var expected = new InterpreterInfo(
                ImmutableList<string>.Empty
                    .Add("using System;")
                    .Add("using System.Collections.Generic;"),
                "Ploeh.Samples.BookingApi",
                "ReservationInstruction",
                ImmutableList<Instruction>.Empty
                    .Add(
                        new Instruction(
                            "IsReservationInFuture",
                            "bool",
                            ImmutableList<Property>.Empty
                                .Add(new Property("Reservation", "Reservation"))
                        )
                    )
                    .Add(
                        new Instruction(
                            "ReadReservations",
                            "IReadOnlyCollection<Reservation>",
                            ImmutableList<Property>.Empty
                                .Add(new Property("DateTimeOffset", "Date"))
                        )
                    )
                    .Add(
                        new Instruction(
                            "CreateReservation",
                            "int",
                            ImmutableList<Property>.Empty
                                .Add(new Property("Reservation", "Reservation"))
                        )
                    )
            );
            info.Should().Be(expected);
        }
    }
}
