using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DgraphDotNet.Transactions {

    /// <summary>
    /// Use transactions like :
    ///     
    /// <code>  
    /// using(var txn = client.NewTransaction()) {
    ///    txn.mutate
    ///    txn.query
    ///    txn.mutate
    ///    txn.commit
    /// } 
    /// </code>
    ///
    /// or
    ///
    /// <code>  
    /// var txn = client.NewTransaction()
    /// txn.mutate()
    /// if(...) {
    ///    txn.commit();
    /// } else {
    ///    txn.discard();
    /// }
    /// </code>
    /// </summary>
    public interface ITransaction : IDisposable, IQuery {
        
        TransactionState TransactionState { get; }

        /// <summary>
        /// Any JSON add mutation. See https://docs.dgraph.io/mutations/#json-mutation-format
        /// </summary>
        Task<FluentResults.Result<IDictionary<string, string>>> Mutate(string json);

        /// <summary>
        /// Any JSON delete mutation. See https://docs.dgraph.io/mutations/#json-mutation-format
        /// </summary>
        Task<FluentResults.Result> Delete(string json);

        Task Discard();
        
        Task<FluentResults.Result> Commit();
    }

    public enum TransactionState { OK, Committed, Aborted, Error }
}