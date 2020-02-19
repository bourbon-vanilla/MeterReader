using Google.Protobuf.WellKnownTypes;
using MeterReaderWeb.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MeterReaderClient
{
    internal class ReadingFactory
    {
        private readonly ILogger<ReadingFactory> _logger;

        public ReadingFactory(ILogger<ReadingFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public Task<ReadingMessage> Generate(int customerId)
        {
            return Task.FromResult(new ReadingMessage
            {
                CustomerId = customerId,
                ReadingTime = Timestamp.FromDateTime(DateTime.UtcNow),
                ReadingValue = new Random().Next(10000)
            });
        }
    }
}