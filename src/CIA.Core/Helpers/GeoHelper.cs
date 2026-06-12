using System;
using System.Collections.Generic;
using CIA.Core.Constants;
using CIA.Core.DTOs;

namespace CIA.Core.Helpers
{
    public static class GeoHelper
    {
        /// <summary>
        /// Calculates the distance between two geographic points using the Haversine formula.
        /// </summary>
        public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return AppConstants.EarthRadiusKm * c;
        }

        /// <summary>
        /// Calculates the bearing from point 1 to point 2 in degrees (0-360).
        /// </summary>
        public static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
        {
            var dLon = ToRadians(lon2 - lon1);
            var lat1Rad = ToRadians(lat1);
            var lat2Rad = ToRadians(lat2);

            var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
            var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                    Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

            var bearing = ToDegrees(Math.Atan2(y, x));
            return (bearing + 360) % 360;
        }

        /// <summary>
        /// Calculates a destination point given a starting point, bearing, and distance.
        /// </summary>
        public static GeoPointDto CalculateDestination(double lat, double lon, double bearingDegrees, double distanceKm)
        {
            var latRad = ToRadians(lat);
            var lonRad = ToRadians(lon);
            var bearingRad = ToRadians(bearingDegrees);
            var angularDistance = distanceKm / AppConstants.EarthRadiusKm;

            var destLat = Math.Asin(
                Math.Sin(latRad) * Math.Cos(angularDistance) +
                Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(bearingRad));

            var destLon = lonRad + Math.Atan2(
                Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(latRad),
                Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(destLat));

            return new GeoPointDto(ToDegrees(destLat), ToDegrees(destLon));
        }

        /// <summary>
        /// Generates a sector polygon (pie slice) for a given base station sector.
        /// </summary>
        public static List<GeoPointDto> GenerateSectorPolygon(
            double centerLat, double centerLon,
            double azimuth, double beamWidthDegrees,
            double radiusKm, int pointCount = 20)
        {
            var polygon = new List<GeoPointDto>();
            polygon.Add(new GeoPointDto(centerLat, centerLon));

            var startAngle = azimuth - beamWidthDegrees / 2;
            var endAngle = azimuth + beamWidthDegrees / 2;
            var step = beamWidthDegrees / pointCount;

            for (var angle = startAngle; angle <= endAngle; angle += step)
            {
                var normalizedAngle = (angle + 360) % 360;
                var point = CalculateDestination(centerLat, centerLon, normalizedAngle, radiusKm);
                polygon.Add(point);
            }

            polygon.Add(new GeoPointDto(centerLat, centerLon));
            return polygon;
        }

        /// <summary>
        /// Checks if a point is within a sector polygon.
        /// </summary>
        public static bool IsPointInSector(
            double pointLat, double pointLon,
            double centerLat, double centerLon,
            double azimuth, double beamWidthDegrees,
            double radiusKm)
        {
            var distance = CalculateDistanceKm(centerLat, centerLon, pointLat, pointLon);
            if (distance > radiusKm) return false;

            var bearing = CalculateBearing(centerLat, centerLon, pointLat, pointLon);
            var halfBeam = beamWidthDegrees / 2;
            var minAngle = (azimuth - halfBeam + 360) % 360;
            var maxAngle = (azimuth + halfBeam + 360) % 360;

            if (minAngle <= maxAngle)
                return bearing >= minAngle && bearing <= maxAngle;
            else
                return bearing >= minAngle || bearing <= maxAngle;
        }

        /// <summary>
        /// Checks if a point is within a bounding box.
        /// </summary>
        public static bool IsPointInBoundingBox(
            double pointLat, double pointLon,
            double minLat, double maxLat,
            double minLon, double maxLon)
        {
            return pointLat >= minLat && pointLat <= maxLat &&
                   pointLon >= minLon && pointLon <= maxLon;
        }

        /// <summary>
        /// Calculates the centroid of a list of geographic points.
        /// </summary>
        public static GeoPointDto CalculateCentroid(List<GeoPointDto> points)
        {
            if (points == null || points.Count == 0)
                return new GeoPointDto(0, 0);

            double sumLat = 0, sumLon = 0;
            foreach (var p in points)
            {
                sumLat += p.Latitude;
                sumLon += p.Longitude;
            }

            return new GeoPointDto(sumLat / points.Count, sumLon / points.Count);
        }

        /// <summary>
        /// Estimates coverage radius using simplified Okumura-Hata model.
        /// </summary>
        public static double EstimateCoverageRadiusKm(
            double txPowerDbm,
            double antennaHeightM,
            double frequencyMhz,
            double minRsrpDbm = -110.0,
            Enums.TerrainType terrainType = Enums.TerrainType.Urban)
        {
            // Simplified path loss model
            double pathLossDb = txPowerDbm - minRsrpDbm;

            // Okumura-Hata simplified for urban
            double a_hm = (1.1 * Math.Log10(frequencyMhz) - 0.7) * 1.5 -
                          (1.56 * Math.Log10(frequencyMhz) - 0.8);

            double terrainFactor = 0;
            switch (terrainType)
            {
                case Enums.TerrainType.DenseUrban: terrainFactor = 3.0; break;
                case Enums.TerrainType.Urban: terrainFactor = 0.0; break;
                case Enums.TerrainType.SubUrban: terrainFactor = -2.0; break;
                case Enums.TerrainType.Rural: terrainFactor = -4.78 * Math.Pow(Math.Log10(frequencyMhz), 2) + 18.33 * Math.Log10(frequencyMhz) - 40.94; break;
                case Enums.TerrainType.OpenArea: terrainFactor = -4.78 * Math.Pow(Math.Log10(frequencyMhz), 2) + 18.33 * Math.Log10(frequencyMhz) - 35.94; break;
            }

            double L0 = 69.55 + 26.16 * Math.Log10(frequencyMhz) -
                        13.82 * Math.Log10(antennaHeightM) - a_hm + terrainFactor;

            double logD = (pathLossDb - L0) / (44.9 - 6.55 * Math.Log10(antennaHeightM));
            double distanceKm = Math.Pow(10, logD);

            return Math.Max(AppConstants.MinCoverageRadiusKm,
                   Math.Min(AppConstants.MaxCoverageRadiusKm, distanceKm));
        }

        /// <summary>
        /// Estimates RSRP at a given distance using simplified path loss model.
        /// </summary>
        public static double EstimateRsrpAtDistance(
            double txPowerDbm,
            double antennaHeightM,
            double frequencyMhz,
            double distanceKm,
            Enums.TerrainType terrainType = Enums.TerrainType.Urban)
        {
            if (distanceKm <= 0) return txPowerDbm;

            double a_hm = (1.1 * Math.Log10(frequencyMhz) - 0.7) * 1.5 -
                          (1.56 * Math.Log10(frequencyMhz) - 0.8);

            double terrainFactor = 0;
            switch (terrainType)
            {
                case Enums.TerrainType.DenseUrban: terrainFactor = 3.0; break;
                case Enums.TerrainType.Urban: terrainFactor = 0.0; break;
                case Enums.TerrainType.SubUrban: terrainFactor = -2.0; break;
                case Enums.TerrainType.Rural: terrainFactor = -4.78 * Math.Pow(Math.Log10(frequencyMhz), 2) + 18.33 * Math.Log10(frequencyMhz) - 40.94; break;
                case Enums.TerrainType.OpenArea: terrainFactor = -4.78 * Math.Pow(Math.Log10(frequencyMhz), 2) + 18.33 * Math.Log10(frequencyMhz) - 35.94; break;
            }

            double pathLoss = 69.55 + 26.16 * Math.Log10(frequencyMhz) -
                              13.82 * Math.Log10(antennaHeightM) - a_hm +
                              (44.9 - 6.55 * Math.Log10(antennaHeightM)) * Math.Log10(distanceKm) +
                              terrainFactor;

            return txPowerDbm - pathLoss;
        }

        /// <summary>
        /// Checks if a point is inside a polygon using ray casting algorithm.
        /// </summary>
        public static bool IsPointInPolygon(double lat, double lon, List<GeoPointDto> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;

            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; i++)
            {
                if ((polygon[i].Longitude > lon) != (polygon[j].Longitude > lon) &&
                    lat < (polygon[j].Latitude - polygon[i].Latitude) *
                    (lon - polygon[i].Longitude) /
                    (polygon[j].Longitude - polygon[i].Longitude) + polygon[i].Latitude)
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
        private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
    }
}
