# Introduction

IOTA is a distributed ledger for the Internet of Things. The first ledger with microtransactions without fees as well as secure data transfer. Quantum proof.
http://iota.org

## Nuget

```PowerShell
Install-Package Borlay.Iota.Library
```

## Getting started

Get address with balance and transactions hashes
```cs
var api = new IotaApi("http://node.iotawallet.info:14265", 15);
var address = await api.GetAddress("YOURSEED", 0);

// use address
var balance = address.Balance;
var transactionHashes = address.Transactions;
```

 Renew your addresses
 ```cs
api.RenewBalances(address); // gets balances
api.RenewTransactions(address); // gets transactions hashes
api.RenewAddresses(address); // both
```

You can send empty transaction simply by doing this
```cs
var transfer = new TransferItem()
{
  Address = "ADDRESS",
  Value = 0,
  Message = "MESSAGETEST",
  Tag = "TAGTEST"
};
var transactionItem = await api.SendTransfer(transfer, CancellationToken.None);
```

Or you can send transaction with value
```cs
var transfer = new TransferItem()
{
  Address = "ADDRESS",
  Value = 1000,
  Message = "MESSAGETEST",
  Tag = "TAGTEST"
};
var transactionItem = await api.SendTransfer(transfer, "YOURSEED", 0, CancellationToken.None);
```

# POW

You can do pow (attachToTangle) like this
```cs
var transactionTrytes = transfer.CreateTransactions().GetTrytes(); // gets transactions from transfer and then trytes
var toApprove = await IriApi.GetTransactionsToApprove(9); // gets transactions to approve
var trunk = toApprove.TrunkTransaction;
var branch = toApprove.BranchTransaction;

var trytesToSend = await transactionTrytes
                .DoPow(trunk, branch, 15, 0, CancellationToken.None); // do pow
await BroadcastAndStore(trytesToSend); // broadcast and send trytes
```

Official IOTA project here: https://github.com/iotaledger
