using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MeterReaderWeb.Data;
using MeterReaderWeb.Data.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MeterReaderWeb.Services
{
    public class MeterService : MeaterReadingService.MeaterReadingServiceBase
    {
        private readonly ILogger<MeterService> _logger;
        private readonly IReadingRepository _readingRepository;

        public MeterService(ILogger<MeterService> logger, IReadingRepository readingRepository)
        {
            _logger = logger;
            _readingRepository = readingRepository;
        }

        public async override Task<Empty> Test(Empty request, ServerCallContext context)
        {
            return await base.Test(request, context);
        }

        public async override Task<Empty> SendDiagnostics(IAsyncStreamReader<ReadingMessage> requestStream, ServerCallContext context)
        {
            var task = Task.Run(async () =>
            {
                await foreach(var reading in requestStream.ReadAllAsync())
                {
                    _logger.LogInformation($"Received Reading: {reading}");
                }
            });

            await task;

            return new Empty();
        }

        public async override Task<StatusMessage> AddReading(ReadingPaket request, ServerCallContext context)
        {
            var result = new StatusMessage { Success = ReadingStatus.Failure };

            if (request.Successful == ReadingStatus.Success)
            {
                try
                {
                    foreach(var r in request.Readings)
                    {
                        if (r.ReadingValue < 1000)
                        {
                            _logger.LogDebug("Reading Value below acceptable level");
                            var trailer = new Metadata()
                            {
                                { "BadValue", r.ReadingValue.ToString() },
                                { "Field", nameof(r.ReadingValue) },
                                { "Message", "Reading is to low" }
                            };
                            throw new RpcException(new Status(StatusCode.OutOfRange, "Value too low"), trailer);
                        }

                        var reading = new MeterReading
                        {
                            Value = r.ReadingValue,
                            ReadingDate = r.ReadingTime.ToDateTime(),
                            CustomerId = r.CustomerId
                        };

                        _readingRepository.AddEntity(reading);

                        if (await _readingRepository.SaveAllAsync())
                        {
                            _logger.LogInformation($"Stored {request.Readings.Count} new readings...");
                            result.Success = ReadingStatus.Success;
                        }
                    }
                }
                catch (RpcException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Xception during add reading action: {ex}");
                    throw new RpcException(Status.DefaultCancelled, "Exception thrown during process");
                }
            }

            return result;
        }
    }
}
