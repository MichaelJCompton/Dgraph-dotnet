using System;
using System.Collections.Generic;
using System.Diagnostics;
using Api;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;
using FluentResults;
using Grpc.Core;

namespace DgraphDotNet {

    internal class Mutation : IMutation {
        private readonly DgraphRequestClient client;
        private TransactionWithMutations transaction;

        private readonly string XIDPropertyName = "xid";

        // Each request instance is a front for a Protos.request.
        private readonly Api.Mutation mutation = new Api.Mutation();
        private Api.Mutation ApiMutation => mutation;

        /// <summary>
        /// Create a request tied to a given client.
        /// </summary>
        internal Mutation(DgraphRequestClient client) {
            this.client = client;
        }

        /// <summary>
        /// Create a request in a given transaction.
        /// </summary>
        internal Mutation(TransactionWithMutations transaction) {
            client = transaction.Client;
            this.transaction = transaction;
        }

        public void AddEdge(Edge edge) {
            if (edge != null) {
                DealWithEdge(edge, (nquad) => { ApiMutation.Set.Add(nquad); });
            }
        }

        public void AddProperty(Property property) {
            if (property != null) {
                DealWithProperty(property, (nquad) => { ApiMutation.Set.Add(nquad); });
            }
        }

        public void DeleteEdge(Edge edge) {
            if (edge != null) {
                DealWithEdge(edge, (nquad) => { ApiMutation.Del.Add(nquad); });
            }
        }

        public void DeleteProperty(Property property) {
            if (property != null) {
                DealWithProperty(property, (nquad) => { ApiMutation.Del.Add(nquad); });
            }
        }

        public int NumAdditions => ApiMutation.Set.Count;

        public int NumDeletions => ApiMutation.Del.Count;

        public FluentResults.Result<IDictionary<string, string>> Submit() {
            return Submit(transaction);
        }

        /// <summary>
        /// Submit to a particular transaction (used internally, better to get a mutation
        /// from the current transaction and use <see cref="Mutation.Submit()"/>).
        /// </summary>
        internal FluentResults.Result<IDictionary<string, string>> Submit(TransactionWithMutations transaction) {
            return transaction.Mutate(ApiMutation);
        }

        internal(List<Edge>, List<Property>) AllAddLinks() {
            List<Edge> edges = new List<Edge>();
            List<Property> properties = new List<Property>();

            foreach (NQuad nquad in ApiMutation.Set) {
                AddFromNQuad(nquad, edges, properties);
            }

            return (edges, properties);
        }

        internal(List<Edge>, List<Property>) AllDeleteLinks() {
            List<Edge> edges = new List<Edge>();
            List<Property> properties = new List<Property>();

            foreach (NQuad nquad in ApiMutation.Del) {
                AddFromNQuad(nquad, edges, properties);
            }

            return (edges, properties);
        }

        private void AddFromNQuad(NQuad nquad, List<Edge> edges, List<Property> properties) {
            if (nquad.ObjectId != null) {
                INode source = nquad.ObjectId.StartsWith("_:")
                    ? (INode) new BlankNode(nquad.ObjectId)
                    : (INode) new NamedNode(Convert.ToUInt64(nquad.ObjectId), "Unknown");
                INode target = nquad.ObjectId.StartsWith("_:")
                    ? (INode) new BlankNode(nquad.ObjectId)
                    : (INode) new NamedNode(Convert.ToUInt64(nquad.ObjectId), "Unknown");

                edges.Add(client.BuildEdge(source, nquad.Predicate, target).Value);
            } else {
                INode source = nquad.ObjectId.StartsWith("_:")
                    ? (INode) new BlankNode(nquad.ObjectId)
                    : (INode) new NamedNode(Convert.ToUInt64(nquad.ObjectId), "Unknown");
                properties.Add(client.BuildProperty(source, nquad.Predicate, GraphValue.BuildFromValue(nquad.ObjectValue)).Value);
            }
        }

        // 
        // ------------------------------------------------------
        //              Privates
        // ------------------------------------------------------
        //
        #region helpers

        private void DealWithEdge(Edge edge, Action<NQuad> placeNQuad) {
            NQuad nquad = BuildNQuadFromEdge(edge);
            placeNQuad(nquad);

            AddXID(edge.Source);
            AddXID(edge.Target);
        }

        private NQuad BuildNQuadFromEdge(Edge edge) {
            Debug.Assert(edge != null);

            NQuad nquad = new NQuad();
            nquad.Subject = GetNodeName(edge.Source);
            nquad.Predicate = edge.Name;
            nquad.ObjectId = GetNodeName(edge.Target);

            return nquad;
        }

        private void DealWithProperty(Property property, Action<NQuad> placeNQuad) {
            NQuad nquad = BuildNQuadFromProperty(property);
            placeNQuad(nquad);

            AddXID(property.Source);

        }

        private NQuad BuildNQuadFromProperty(Property property) {
            Debug.Assert(property != null);

            NQuad nquad = new NQuad();
            nquad.Subject = GetNodeName(property.Source);
            nquad.Predicate = property.Name;
            nquad.ObjectValue = property.Target.Value;

            return nquad;
        }

        private string GetNodeName(INode node) {
            switch (node) {
                case BlankNode bnode:
                    return bnode.BlankNodeName;
                case NamedNode namedNode:
                    return namedNode.UID.ToString();
            }
            return null;
        }
        private void AddXID(INode node) {
            switch (node) {
                case XIDNode xidNode:
                    if (!xidNode.XIDAdded) {
                        var xidEdge = client.BuildProperty(node, XIDPropertyName, GraphValue.BuildStringValue(xidNode.XID));
                        if (xidEdge.IsSuccess) {
                            xidNode.XIDWasAdded(); // gotta do this first or infinite adding xid loop
                            AddProperty(xidEdge.Value);
                        }
                        // BuildProperty should never fail here.  It can only fail if the args are wrong.
                    }
                    break;
            }
        }

        #endregion

    }
}