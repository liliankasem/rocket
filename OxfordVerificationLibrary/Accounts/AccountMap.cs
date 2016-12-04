namespace com.mtaulty.OxfordVerify.Accounts
{
  using Newtonsoft.Json;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Threading.Tasks;
  using Windows.Storage;
  using System.Linq;

  static class AccountMap
  {
    static AccountMap()
    {
      accountMap = new Dictionary<string, Guid>();
    }
    public static async Task<List<string>> GetUserNamesAsync()
    {
      await LoadAsync();
      return (accountMap.Keys.ToList());
    }
    private static async Task LoadAsync()
    {
      if (!loaded)
      {
        try
        {
          var file = await ApplicationData.Current.LocalFolder.GetFileAsync(
            FILENAME);

          var text = await FileIO.ReadTextAsync(file);

          var deserialized = JsonConvert.DeserializeObject<
            Dictionary<string, Guid>>(text);

          accountMap = deserialized;
        }
        catch (FileNotFoundException)
        {
        }
        loaded = true;
      }
    }
    public static async Task<Guid?> GetGuidForUserNameAsync(string userName)
    {
      Guid? guid = null;

      await LoadAsync();

      if (accountMap.ContainsKey(userName))
      {
        guid = accountMap[userName];
      }
      return (guid);
    }
    public static async Task SetGuidForUserNameAsync(string userName,
      Guid guid)
    {
      await LoadAsync();

      accountMap[userName] = guid;

      var serialized = JsonConvert.SerializeObject(accountMap);

      var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
        FILENAME, CreationCollisionOption.ReplaceExisting);

      await FileIO.WriteTextAsync(file, serialized);
    }
    static bool loaded;
    static readonly string FILENAME = "map.json";
    static Dictionary<string, Guid> accountMap;
  }
}