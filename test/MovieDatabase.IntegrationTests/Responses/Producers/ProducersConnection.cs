namespace MovieDatabase.IntegrationTests.Responses.Producers;

public record ProducersConnection(
    List<ProducerQueryDto> Nodes
);