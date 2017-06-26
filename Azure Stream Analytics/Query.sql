WITH [StreamData] AS (
SELECT *
FROM
[IoTHub])

SELECT
    messageId,
    deviceId,
    temperature,
    humidity
INTO
    [TelemetryTable]
FROM
    [StreamData]


SELECT
    messageId,
    deviceId,
    temperature,
    humidity,
    System.Timestamp time
INTO
    [PowerBIStream]
FROM
    [StreamData]

SELECT
    deviceId,
    AVG(temperature) AS temperature,
    AVG(humidity) AS humidity,
    System.Timestamp time
INTO
    [TelemetrySummary]
FROM
    [StreamData]
GROUP BY
    deviceId,
    TumblingWindow(second,10)