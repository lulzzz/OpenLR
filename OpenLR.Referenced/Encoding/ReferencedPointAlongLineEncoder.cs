﻿using NetTopologySuite.LinearReferencing;
using OpenLR.Locations;
using OpenLR.Model;
using OpenLR.Referenced.Locations;
using OsmSharp.Math.Geo;
using OsmSharp.Math.Primitives;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.Routing;
using OsmSharp.Units.Distance;
using System;
using System.Collections.Generic;

namespace OpenLR.Referenced.Encoding
{
    /// <summary>
    /// Represents a referenced point along line location decoder.
    /// </summary>
    public class ReferencedPointAlongLineEncoder : ReferencedEncoder<ReferencedPointAlongLine, PointAlongLineLocation>
    {
        /// <summary>
        /// Creates a point along line referenced encoder.
        /// </summary>
        /// <param name="mainEncoder"></param>
        /// <param name="rawEncoder"></param>
        public ReferencedPointAlongLineEncoder(ReferencedEncoderBase mainEncoder, OpenLR.Encoding.LocationEncoder<PointAlongLineLocation> rawEncoder)
            : base(mainEncoder, rawEncoder)
        {

        }

        /// <summary>
        /// Encodes a point along location.
        /// </summary>
        /// <param name="referencedLocation"></param>
        /// <returns></returns>
        public override PointAlongLineLocation EncodeReferenced(ReferencedPointAlongLine referencedLocation)
        {
            try
            {
                // Step – 1: Check validity of the location and offsets to be encoded.
                // validate connected and traversal.
                referencedLocation.Route.ValidateConnected(this.MainEncoder);
                // validate offsets.
                referencedLocation.Route.ValidateOffsets(this.MainEncoder);
                // validate for binary.
                referencedLocation.Route.ValidateBinary(this.MainEncoder);

                // Step – 2 Adjust start and end node of the location to represent valid map nodes.
                referencedLocation.Route.AdjustToValidPoints(this.MainEncoder);
                // keep a list of LR-point.
                var points = new List<int>(new int[] { 0, referencedLocation.Route.Vertices.Length - 1 });

                // Step – 3     Determine coverage of the location by a shortest-path.
                // Step – 4     Check whether the calculated shortest-path covers the location completely. 
                //              Go to step 5 if the location is not covered completely, go to step 7 if the location is covered.
                // Step – 5     Determine the position of a new intermediate location reference point so that the part of the 
                //              location between the start of the shortest-path calculation and the new intermediate is covered 
                //              completely by a shortest-path.
                // Step – 6     Go to step 3 and restart shortest path calculation between the new intermediate location reference 
                //              point and the end of the location.
                // Step – 7     Concatenate the calculated shortest-paths for a complete coverage of the location and form an 
                //              ordered list of location reference points (from the start to the end of the location).

                // Step – 8     Check validity of the location reference path. If the location reference path is invalid then go 
                //              to step 9, if the location reference path is valid then go to step 10.
                // Step – 9     Add a sufficient number of additional intermediate location reference points if the distance 
                //              between two location reference points exceeds the maximum distance. Remove the start/end LR-point 
                //              if the positive/negative offset value exceeds the length of the corresponding path.
                referencedLocation.Route.AdjustToValidDistances(this.MainEncoder, points);

                // Step – 10    Create physical representation of the location reference.
                var location = new PointAlongLineLocation();

                // match fow/frc for first edge.
                FormOfWay fow;
                FunctionalRoadClass frc;
                var tags = this.GetTags(referencedLocation.Route.Edges[0].Tags);
                if(!this.TryMatching(tags, out frc, out fow))
                {
                    throw new ReferencedEncodingException(referencedLocation, "Could not find frc and/or fow for the given tags.");
                }
                location.First = new Model.LocationReferencePoint();
                location.First.Coordinate = this.GetVertexLocation(referencedLocation.Route.Vertices[0]);
                location.First.FormOfWay = fow;
                location.First.FuntionalRoadClass = frc;
                location.First.LowestFunctionalRoadClassToNext = location.First.FuntionalRoadClass;

                // match for last edge.
                tags = this.GetTags(referencedLocation.Route.Edges[referencedLocation.Route.Edges.Length - 1].Tags);
                if (!this.TryMatching(tags, out frc, out fow))
                {
                    throw new ReferencedEncodingException(referencedLocation, "Could not find frc and/or fow for the given tags.");
                }
                location.Last = new Model.LocationReferencePoint();
                location.Last.Coordinate = this.GetVertexLocation(referencedLocation.Route.Vertices[referencedLocation.Route.Vertices.Length - 1]);
                location.Last.FormOfWay = fow;
                location.Last.FuntionalRoadClass = frc;

                // initialize from point, to point and create the coordinate list.
                var from = new GeoCoordinate(location.First.Coordinate.Latitude, location.First.Coordinate.Longitude);
                var to = new GeoCoordinate(location.Last.Coordinate.Latitude, location.Last.Coordinate.Longitude);
                var coordinates = referencedLocation.Route.GetCoordinates(this.MainEncoder);

                // calculate bearing.
                location.First.Bearing = (int)this.GetBearing(referencedLocation.Route.Vertices[0], referencedLocation.Route.Edges[0],
                    referencedLocation.Route.EdgeShapes[0], referencedLocation.Route.Vertices[1], false).Value;
                location.Last.Bearing = (int)this.GetBearing(referencedLocation.Route.Vertices[referencedLocation.Route.Vertices.Length - 1],
                    referencedLocation.Route.Edges[referencedLocation.Route.Edges.Length - 1], referencedLocation.Route.EdgeShapes[referencedLocation.Route.Edges.Length - 1], 
                    referencedLocation.Route.Vertices[referencedLocation.Route.Vertices.Length - 2], true).Value;

                // calculate length.
                var lengthInMeter = coordinates.Length();
                location.First.DistanceToNext = (int)lengthInMeter.Value;

                var refLength = 0.0;
                for(int i = 0; i < referencedLocation.Route.Edges.Length;i++)
                {
                    refLength = refLength + referencedLocation.Route.Edges[i].Distance;
                    var test = referencedLocation.Route.GetCoordinates(this.MainEncoder, 0, i + 2);
                    var testLength = test.Length();
                }

                // calculate orientation and side of road.
                PointF2D bestProjected;
                LinePointPosition bestPosition;
                Meter bestOffset;
                if (!coordinates.ProjectOn(new PointF2D(referencedLocation.Longitude, referencedLocation.Latitude), out bestProjected, out bestPosition, out bestOffset))
                { // the projection on the edge failed.
                    throw new ReferencedEncodingException(referencedLocation, "The point in the ReferencedPointAlongLine could not be projected on the referenced edge.");
                }

                location.Orientation = referencedLocation.Orientation;
                switch (bestPosition)
                {
                    case global::OsmSharp.Math.Primitives.LinePointPosition.Left:
                        location.SideOfRoad = SideOfRoad.Left;
                        break;
                    case global::OsmSharp.Math.Primitives.LinePointPosition.On:
                        location.SideOfRoad = SideOfRoad.OnOrAbove;
                        break;
                    case global::OsmSharp.Math.Primitives.LinePointPosition.Right:
                        location.SideOfRoad = SideOfRoad.Right;
                        break;
                }

                // calculate offset.
                location.PositiveOffsetPercentage = (float)(bestOffset.Value / lengthInMeter.Value) * 100.0f;
                if(location.PositiveOffsetPercentage >= 100)
                { // should be in the range of [0-100[.
                    // encoding should always work even if not 100% accurate in this case.
                    location.PositiveOffsetPercentage = 99;
                }

                return location;
            }
            catch (ReferencedEncodingException)
            { // rethrow referenced encoding exception.
                throw;
            }
            catch (Exception ex)
            { // unhandled exception!
                throw new ReferencedEncodingException(referencedLocation,
                    string.Format("Unhandled exception during ReferencedPointAlongLineEncoder: {0}", ex.ToString()), ex);
            }
        }
    }
}