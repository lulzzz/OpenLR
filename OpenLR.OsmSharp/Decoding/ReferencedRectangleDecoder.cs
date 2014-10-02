﻿using OpenLR.Locations;
using OpenLR.OsmSharp.Locations;
using OpenLR.OsmSharp.Router;
using OsmSharp.Routing.Graph;
using OsmSharp.Routing.Graph.Router;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenLR.OsmSharp.Decoding
{
    /// <summary>
    /// Represents a referenced rectangle location decoder.
    /// </summary>
    /// <typeparam name="TEdge"></typeparam>
    public class ReferencedRectangleDecoder<TEdge> : ReferencedDecoder<ReferencedRectangle, RectangleLocation, TEdge>
        where TEdge : IDynamicGraphEdgeData
    {
        /// <summary>
        /// Creates a rectangle location graph decoder.
        /// </summary>
        /// <param name="mainDecoder"></param>
        /// <param name="rawDecoder"></param>
        public ReferencedRectangleDecoder(ReferencedDecoderBase<TEdge> mainDecoder, OpenLR.Decoding.LocationDecoder<RectangleLocation> rawDecoder)
            : base(mainDecoder, rawDecoder)
        {

        }

        /// <summary>
        /// Decodes an OpenLR-encoded unreferenced raw OpenLR location into a referenced Location.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public override ReferencedRectangle Decode(RectangleLocation location)
        {
            return new ReferencedRectangle()
            {
                LowerLeftLatitude = location.LowerLeft.Latitude,
                LowerLeftLongitude = location.LowerLeft.Longitude,
                UpperRightLatitude = location.UpperRight.Latitude,
                UpperRightLongitude = location.UpperRight.Longitude
            };
        }
    }
}