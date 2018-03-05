using System;
using NUnit.Framework;

using DgraphDotNet;
using DgraphDotNet.Graph;
using DgraphDotNet.Transactions;

using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace Dgraph_dotnet.tests.Client
{
    public class MutationTests
    {
        IDgraphMutationsClient client;

        [SetUp]
        public void Setup()
        {
            client = Substitute.For<IDgraphMutationsClient>();
        }

        [Test]
        public void NumAdditonsIsCorrect()
        {
            Mutation mut = new DgraphDotNet.Mutation();

            var n1 = new UIDNode(1);
            var n2 = new UIDNode(2);
            Edge edge = new Edge(n1, "AnEdge", n2);
            Property property = new Property(n1, "AProperty", GraphValue.BuildBoolValue(true));

            mut.AddEdge(edge);
            mut.AddProperty(property);

            Assert.AreEqual(2, mut.NumAdditions);
        }

        [Test]
        public void NumDeletionsIsCorrect()
        {
            Mutation mut = new DgraphDotNet.Mutation();

            Edge edge = new Edge(new NamedNode(1, "N1"), "AnEdge", new NamedNode(2, "N2"));
            Property property = new Property(new NamedNode(1, "N1"), "AProperty", GraphValue.BuildBoolValue(true));

            mut.DeleteEdge(edge);
            mut.DeleteProperty(property);

            Assert.AreEqual(2, mut.NumDeletions);
        }

        [Test]
        public void MutationContainsAllSubmittedEdges()
        {
            List<Edge> edges = new List<Edge>();
            List<Property> properties = new List<Property>();
            var transaction = Substitute.For<ITransactionWithMutations>();
            IMutation mut = new Mutation();

            for (int i = 0; i < 10; i++)
            {
                edges.Add(new Edge(new NamedNode((ulong)i, "N"+i), "AnEdge", new NamedNode((ulong)i + 1, "N"+i)));
                properties.Add(new Property(new NamedNode((ulong)i, "N"+i), "AnEdge", GraphValue.BuildIntValue(i)));
            }

            for (int i = 0; i < 5; i++)
            {
                mut.AddEdge(edges[i]);
                mut.AddProperty(properties[i]);
            }

            for (int i = 5; i < 10; i++)
            {
                mut.DeleteEdge(edges[i]);
                mut.DeleteProperty(properties[i]);
            }

            transaction.When(x => x.ApiMutate(Arg.Any<Api.Mutation>()))
            .Do(x =>
            {
                Assert.True(AllEdgesInMutation(edges, x.Arg<Api.Mutation>()));
            });

            mut.SubmitTo(transaction);
        }

        private bool AllEdgesInMutation(List<Edge> edges, Api.Mutation mutation)
        {
            return edges.All(
                edge => mutation.Set.Any(nquad => Edge_EQ_NQuad(edge, nquad)) || 
                    mutation.Del.Any(nquad => Edge_EQ_NQuad(edge, nquad)));
        }

        private bool AllPropertiesInMutation(List<Property> properties, Api.Mutation mutation)
        {
            return properties.All(
                property => mutation.Set.Any(nquad => Property_EQ_NQuad(property, nquad)) || 
                    mutation.Del.Any(nquad => Property_EQ_NQuad(property, nquad)));
        }


        private bool Edge_EQ_NQuad(Edge edge, Api.NQuad nquad)
        {
            return
            nquad.Subject.Equals(((NamedNode)edge.Source).UID.ToString(), StringComparison.Ordinal)
            &&
            nquad.Predicate.Equals(edge.Name, StringComparison.Ordinal)
            &&
            nquad.ObjectId.Equals(((NamedNode)edge.Target).UID.ToString(), StringComparison.Ordinal);
        }

        private bool Property_EQ_NQuad(Property property, Api.NQuad nquad)
        {
            return
            nquad.Subject.Equals(((NamedNode)property.Source).UID.ToString(), StringComparison.Ordinal)
            &&
            nquad.Predicate.Equals(property.Name, StringComparison.Ordinal)
            &&
            nquad.ObjectValue.Equals(property.Target.Value);
        }
    }
}