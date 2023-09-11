
#region LİNQ EXTENSİON - partition
public static List<List<T>> partition<T>(this List<T> values, int chunkSize)
{
    return values.Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / chunkSize)
        .Select(x => x.Select(v => v.Value).ToList())
        .ToList();
}
#endregion

#region Thread'den dönecek olan global toplam liste
List<RootProductModel> returnList = new List<RootProductModel>();
#endregion

#region Thread'in global count durumu
public int threadCount;
#endregion

#region Threadi çalıştaracak ana http function
public async Task<List<RootProductModel>> GetImageCDNControlerList()
{   
    List<DctProduct> existProductList = DbSet<DctProduct>().AsNoTracking().Where(x => x.SeasonId == seasonId).ToList();

    #region Bölümlerin oluşacağı listeyi örneğin 50 adet olarak paylaştırcaktır.
    List<List<DctProduct>> partitions = existProductList.ToList().partition(50);
    threadCount = partitions.Count;
    #endregion
             
    #region Bu kısımda oluşan 50 lik bölümlerin argumant olarak thread.start olarak aynı anda başlatılıyor        
    foreach (List<DctProduct> rowList in partitions)
    {
        object[] argumentList = { rowList , activeBrand , fixedLink};
        Thread threadPartition = new Thread(new ParameterizedThreadStart(CDNCheckThread));
        threadPartition.Start(argumentList);
    }
    #endregion

    while (threadCount > 0){};
             
    //global listenin döneceği kısımdır         
    return returnList;
}
#endregion

#region Thread'lerin içinde olan datanın yapacağı işlerin kısımları 
public void CDNCheckThread(object arguments)
{
    object[] args = (object[])arguments;

    List<DctProduct> existProductList = (List<DctProduct>)args[0];
    DctBrand activeBrand = (DctBrand)args[1];
    string fixedLink = (string)args[2];

    foreach (DctProduct tempProduct in existProductList)
    {
        RootProductModel rootProductModel = new RootProductModel();
        String mainCndLink = "";

        if (activeBrand.DcId != 1003)
            {
                if (tempProduct.ColorObject != null)
                    mainCndLink = fixedLink + activeBrand.Name.ToUpper() + "/" + tempProduct.ModelCode + "_" + tempProduct.ColorObject.Code + ".jpg";
                }
                else
                {
                    mainCndLink = fixedLink + activeBrand.Name.ToUpper() + "/" + tempProduct.ModelCode + ".jpg";
                }

                if (mainCndLink != "")
                {
                    WebRequest webRequest = WebRequest.Create(mainCndLink);
                    webRequest.Method = "HEAD";
                    webRequest.Proxy = null;
                    ServicePointManager.Expect100Continue = false;
                    try
                    {
                        webRequest.GetResponse();
                    }
                    catch
                    {
                        rootProductModel.BrandName = activeBrand.Name.ToUpper();
                        rootProductModel.ImageCdnLink = mainCndLink;
                        rootProductModel.ModelCode = tempProduct.ModelCode;
                        returnList.Add(rootProductModel);
                    }
                }         
    }

    threadCount--;
}
#endregion

        