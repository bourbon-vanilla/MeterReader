using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using MeterReaderWeb.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeterReaderClient
{
    internal class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _config;
        private readonly ReadingFactory _readingFactory;
        private MeaterReadingService.MeaterReadingServiceClient _client = null;


        public Worker(ILogger<Worker> logger, IConfiguration config, ReadingFactory readingFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _readingFactory = readingFactory ?? throw new ArgumentNullException(nameof(readingFactory));
        }


        protected MeaterReadingService.MeaterReadingServiceClient Client { get
            {
                if (_client == null)
                {
                    var channel = GrpcChannel.ForAddress(
                        _config["Service:ServerUrl"]);
                    _client = new MeaterReadingService.MeaterReadingServiceClient(channel);
                }
                return _client;
            } }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                var customerId = _config.GetValue<int>("Service:CustomerId");

                var packet = new ReadingPaket
                {
                    Successful = ReadingStatus.Success,
                    Notes = "This is our test"
                };

                for (int i = 0; i < 5; i++)
                {
                    packet.Readings.Add(
                        await _readingFactory.Generate(customerId));
                }

                var result = await Client.AddReadingAsync(packet);
                if (result.Success == ReadingStatus.Success)
                    _logger.LogInformation("Successfully send");
                else _logger.LogInformation("Failed to send");

                await Task.Delay(_config.GetValue<int>("Service:DelayInterval"), stoppingToken);
            }
        }
    }
}
