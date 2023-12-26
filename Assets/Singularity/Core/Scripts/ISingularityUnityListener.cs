using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISingularityUnityListener
{
  void onUserLogout();
  void onUserLogIn(string userData);
  void onDrawerOpen();
  void onDrawerClose();
  void onTransactionApprove(string txData);
  void onTransactionSuccess(string txData);
  void onTransactionFailure(string txData);
}
