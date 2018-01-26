using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using Nethereum.JsonRpc.UnityClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using Nethereum.Signer;
using Nethereum.KeyStore;
using System.IO;
using Nethereum.KeyStore.Crypto;
using NUnit.Framework;

public class NEtheriumFirstTest {

    private const string m_endpoint = "https://mainnet.infura.io/avfisnTPpVCewhs6UzDU";
    private const string m_addr = "";
    private const string m_addrTo = "";
    private const string m_blockHash = "0xb903239f8543d04b5dc1ba6579132b143087c68db1b2168786408fcbce568238";
    private const long m_ether = 1000000000000000000;
    private const long m_gwei = 1000000000;

    private decimal GetEthValue(BigInteger bigInteger)
    {
        return decimal.Parse(bigInteger.ToString()) / 1000000000000000000m;
    }

    [UnityTest]
    public IEnumerator ProtocolVersion()
    {
        EthProtocolVersionUnityRequest req = new EthProtocolVersionUnityRequest(m_endpoint);

        yield return req.SendRequest();

        Debug.Log("protocol version " + req.Result);
    }

    [UnityTest]
    public IEnumerator Syncing()
    {
        EthSyncingUnityRequest req = new EthSyncingUnityRequest(m_endpoint);

        yield return req.SendRequest();

        Debug.Log("syncing " + req.Result);
    }

    [UnityTest]
    public IEnumerator Coibase()
    {
        EthCoinBaseUnityRequest req = new EthCoinBaseUnityRequest(m_endpoint);

        yield return req.SendRequest();

        Debug.Log("coinbase " + req.Result);
    }


    [UnityTest]
    public IEnumerator Mining()
    {
        EthMiningUnityRequest req = new EthMiningUnityRequest(m_endpoint);

        yield return req.SendRequest();

        Debug.Log("mining " + req.Result);
    }

    [UnityTest]
    public IEnumerator Hashrate()
    {
        EthHashrateUnityRequest req = new EthHashrateUnityRequest(m_endpoint);

        yield return req.SendRequest();

        Debug.Log("hashrate " + req.Result);
    }


    [UnityTest]
    public IEnumerator GasPrice()
    {
        EthGasPriceUnityRequest req = new EthGasPriceUnityRequest(m_endpoint);

        yield return req.SendRequest();

        Debug.Log("gasprice " + GetEthValue(req.Result));
    }

    [UnityTest]
    public IEnumerator GetAccounts()
    {
        EthAccountsUnityRequest accounts = new EthAccountsUnityRequest(m_endpoint);

        yield return accounts.SendRequest();

        Debug.Log("accounts count " + accounts.Result.Length);
        foreach(string account in accounts.Result)
        {
            Debug.Log(account);
        }
    }

    [UnityTest]
    public IEnumerator BlockNumber()
    {
        EthBlockNumberUnityRequest req = new EthBlockNumberUnityRequest(m_endpoint);

        yield return req.SendRequest();

        Debug.Log("block number " + req.Result);
    }

    [UnityTest]
    public IEnumerator GetBalance()
    {
        EthGetBalanceUnityRequest req = new EthGetBalanceUnityRequest(m_endpoint);

        yield return req.SendRequest(m_addr, BlockParameter.CreateLatest());

        Debug.Log("balance " + GetEthValue(req.Result.Value));
    }

    [UnityTest]
    public IEnumerator GetStorageAt()
    {
        yield return null;
    }

   
    [UnityTest]
    public IEnumerator GetTransactionCount()
    {
        EthGetTransactionCountUnityRequest req = new EthGetTransactionCountUnityRequest(m_endpoint);

        yield return req.SendRequest(m_addr, BlockParameter.CreateLatest());

        Debug.Log("transaction count " + req.Result.Value);
    }

    [UnityTest]
    public IEnumerator GetBlockTransactionCountByHash()
    {
        EthGetBlockTransactionCountByHashUnityRequest req = new EthGetBlockTransactionCountByHashUnityRequest(m_endpoint);

        yield return req.SendRequest(m_blockHash);

        Debug.Log("number of transactions in a block with hash " + m_blockHash + " " + req.Result.Value);
    }

    [UnityTest]
    public IEnumerator GetBlockTransactionCountByNumber()
    {
        EthGetBlockTransactionCountByNumberUnityRequest req = new EthGetBlockTransactionCountByNumberUnityRequest(m_endpoint);

        yield return req.SendRequest(new BlockParameter(232));

        Debug.Log("number of transactions in a 232 block " + req.Result.Value);
    }

    [UnityTest]
    public IEnumerator GetUncleCountByHash()
    {
        EthGetUncleCountByBlockHashUnityRequest req = new EthGetUncleCountByBlockHashUnityRequest(m_endpoint);

        yield return req.SendRequest(m_blockHash);

        Debug.Log("number of uncles for block with hash " + m_blockHash + " " + req.Result.Value);
    }

    [UnityTest]
    public IEnumerator GetUncleCountByNumber()
    {
        EthGetUncleCountByBlockNumberUnityRequest req = new EthGetUncleCountByBlockNumberUnityRequest(m_endpoint);

        yield return req.SendRequest(new HexBigInteger(new BigInteger(232)));

        Debug.Log("number of uncles for block with number 232 " + req.Result.Value);
    }


    [UnityTest]
    public IEnumerator GetCode()
    {
        EthGetCodeUnityRequest req = new EthGetCodeUnityRequest(m_endpoint);

        yield return req.SendRequest(m_addr, BlockParameter.CreateLatest());

        Debug.Log("code " + req.Result);
    }

    [UnityTest]
    public IEnumerator GetSign()
    {
        EthSignUnityRequest req = new EthSignUnityRequest(m_endpoint);

        yield return req.SendRequest(m_addr, "0xdeadbeaf");

        Debug.Log("sign result " + req.Result);
    }

    [UnityTest]
    public IEnumerator EthCall()
    {
        EthCallUnityRequest req = new EthCallUnityRequest(m_endpoint);

        CallInput input = new CallInput("", m_addrTo, m_addr,
            new HexBigInteger(new BigInteger(21000)),
            new HexBigInteger(new BigInteger(20L * m_gwei)), //gwei
            new HexBigInteger(new BigInteger(0.01f * m_ether))); //ether

        yield return req.SendRequest(input, BlockParameter.CreateLatest());

        Debug.Log("EthCall " + req.Result);
    }

    [UnityTest]
    public IEnumerator SendRawTransaction()
    {
        //unpack first
        string json = File.ReadAllText(@"ks");

        KeyStoreService keystore = new KeyStoreService();

        byte[] privateKey;
        try
        {
            privateKey = keystore.DecryptKeyStoreFromJson("", json);  //live
        }
        catch(DecryptionException exc)
        {
            Debug.Log("password invalid");
            throw exc;
        }
        

        int nonce = 1; // GetPending transaction count + 1;

        TransactionSigner signer = new TransactionSigner();
        string signedTransactionData = signer.SignTransaction(privateKey, m_addr,
              new BigInteger(0.002f * m_ether),
              new BigInteger(nonce),
              new BigInteger(20L * m_gwei),
              new BigInteger(21000));

        Assert.IsTrue(signer.VerifyTransaction(signedTransactionData));

        Debug.Log(signer.GetSenderAddress(signedTransactionData));
        
        EthSendRawTransactionUnityRequest req = new EthSendRawTransactionUnityRequest(m_endpoint);

        yield return req.SendRequest(signedTransactionData);

        Debug.Log(req.Result);
    }
}
