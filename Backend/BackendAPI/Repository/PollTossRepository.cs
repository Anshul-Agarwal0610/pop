using System.Data;
using System.Security.Cryptography;
using System.Text;
using BackendAPI.Data; using BackendAPI.Interfaces; using BackendAPI.Models; using Dapper; using Microsoft.Data.SqlClient;
namespace BackendAPI.Repository;
public sealed class PollTossRepository(DapperContext context) : IPollTossRepository {
 static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
 static string Token()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
 static string Code()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(4))[..6];
 public async Task<(PollTossInvitation,string)> CreateAsync(long pollId,long senderId,DateTime now) {
  using var c=context.CreateConnection(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable);
  var eligible=await c.QuerySingleOrDefaultAsync<int?>(@"SELECT 1 FROM Polls WITH(UPDLOCK,HOLDLOCK) WHERE Id=@PollId AND IsActive=1 AND ExpiresAt>@Now AND ModerationStatus='Published' AND COALESCE(IsPrivate,0)=0 AND COALESCE(IsWellness,0)=0 AND LOWER(Category) NOT LIKE '%health%'",new{PollId=pollId,Now=now},tx);
  if(eligible is null) throw new PollTossException("ineligible","This poll cannot be tossed.");
  var token=Token(); var id=Guid.NewGuid(); var expires=now.AddMinutes(15); string code="";
  var inserted=false; for(var attempt=0;attempt<8;attempt++) { code=Code(); try { await c.ExecuteAsync(@"INSERT INTO PollTossInvitations(Id,PollId,SenderUserId,TokenHash,RoomCode,Status,CreatedAt,ExpiresAt,StateVersion) VALUES(@Id,@PollId,@Sender,@Hash,@Code,'Pending',@Now,@Expires,1)",new{Id=id,PollId=pollId,Sender=senderId,Hash=Hash(token),Code=code,Now=now,Expires=expires},tx); inserted=true; break; } catch(SqlException e) when(e.Number is 2601 or 2627) {} }
  if(!inserted) throw new PollTossException("code_unavailable","Could not allocate a room code. Try again.");
  tx.Commit(); return ((await GetForSenderAsync(id,senderId,now))!,token);
 }
 public Task<PollTossInvitation?> GetForSenderAsync(Guid id,long senderId,DateTime now)=>Read("i.Id=@Id AND i.SenderUserId=@Sender",new{Id=id,Sender=senderId},now,true);
 public Task<PollTossInvitation?> PreviewByTokenAsync(string token,DateTime now)=>Read("i.TokenHash=@Hash",new{Hash=Hash(token)},now,false);
 public Task<PollTossInvitation?> PreviewByRoomCodeAsync(string code,DateTime now)=>Read("i.RoomCode=@Code",new{Code=code.Trim().ToUpperInvariant()},now,false);
 async Task<PollTossInvitation?> Read(string where,object args,DateTime now,bool sender) {
  using var c=context.CreateConnection(); c.Open(); var p=new DynamicParameters(args);p.Add("Now",now);
  await c.ExecuteAsync($"UPDATE PollTossInvitations SET Status='Expired',StateVersion=StateVersion+1 WHERE {where} AND Status='Pending' AND ExpiresAt<=@Now",p);
  var sql=$@"SELECT i.Id,i.PollId,i.Status,i.StateVersion,i.ExpiresAt{(sender ? ",i.RoomCode" : "")},p.Id,p.Question,p.Category,p.ThumbnailUrl FROM PollTossInvitations i JOIN Polls p ON p.Id=i.PollId WHERE {where}";
  PollTossInvitation? found=null; await c.QueryAsync<PollTossInvitation,PollTossPollPreview,PollTossInvitation>(sql,(i,poll)=>{i.Poll=poll;found=i;return i;},p,splitOn:"Id"); return found;
 }
 public async Task<PollTossInvitation> AcceptAsync(string token,long recipientId,DateTime now) {
  using var c=context.CreateConnection(); c.Open(); using var tx=c.BeginTransaction(IsolationLevel.Serializable); var hash=Hash(token);
  var row=await c.QuerySingleOrDefaultAsync<PollTossInvitation>("SELECT * FROM PollTossInvitations WITH(UPDLOCK,ROWLOCK) WHERE TokenHash=@Hash",new{Hash=hash},tx) ?? throw new PollTossException("not_found","Invitation not found.");
  if(row.Status==PollTossStatuses.Accepted && row.RecipientUserId==recipientId){tx.Commit();return (await PreviewByTokenAsync(token,now))!;}
  if(row.Status==PollTossStatuses.Pending && row.ExpiresAt<=now){await c.ExecuteAsync("UPDATE PollTossInvitations SET Status='Expired',StateVersion=StateVersion+1 WHERE Id=@Id",new{row.Id},tx);tx.Commit();throw new PollTossException("expired","Invitation expired.");}
  if(row.Status!=PollTossStatuses.Pending) throw new PollTossException(row.Status.ToLowerInvariant(),"Invitation is no longer available.");
  await c.ExecuteAsync("UPDATE PollTossInvitations SET Status='Accepted',RecipientUserId=@User,AcceptedAt=@Now,StateVersion=StateVersion+1 WHERE Id=@Id",new{User=recipientId,Now=now,row.Id},tx);tx.Commit(); return (await PreviewByTokenAsync(token,now))!;
 }
 public async Task<PollTossInvitation> CancelAsync(Guid id,long senderId,DateTime now) {
  using var c=context.CreateConnection(); var changed=await c.ExecuteAsync("UPDATE PollTossInvitations SET Status='Cancelled',CancelledAt=@Now,StateVersion=StateVersion+1 WHERE Id=@Id AND SenderUserId=@Sender AND Status='Pending' AND ExpiresAt>@Now",new{Id=id,Sender=senderId,Now=now});
  var result=await GetForSenderAsync(id,senderId,now) ?? throw new PollTossException("not_found","Invitation not found."); if(changed==0 && result.Status==PollTossStatuses.Accepted) throw new PollTossException("accepted","Invitation was already accepted."); return result;
 }
}
