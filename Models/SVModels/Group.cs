using System.Text.Json.Serialization;

namespace LoanBroker.Models.SVModels;

public enum GroupTypes
{
    Company = 0,
    // a corporation is a company that is listed on SVSE or a company on a private stock exchange that the CFV has determined is a corporation
    Corporation = 1,
    NonProfit = 2,
    PoliticalParty = 3,
    District = 4,
    State = 5,
    Province = 6
}


public enum GroupFlag
{
    // is only given by the CFV
    NonProfit = 0,
    // is only given by the MOJ
    News = 1,
    CanHaveMilitary = 2,
    // is only given by the CFV
    CanSetTransactionsExpenseStatus = 3,
    // is only given by the CFV
    AccreditedBank = 4
}

public class Group : BaseEntity
{
    [JsonPropertyName("groupType")]
    public GroupTypes GroupType { get; set; }

    [JsonPropertyName("flags")]
    public List<GroupFlag> Flags { get; set; }
}
