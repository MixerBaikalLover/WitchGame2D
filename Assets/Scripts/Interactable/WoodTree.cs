using System.Collections;
using UnityEngine;

public class WoodTree : Interactable
{
    #region Vars

    // Public fields
    public new TreeData Data => (TreeData) data;
    public new TreeSaveData InstanceData => (TreeSaveData) instanceData;

    // Private fields
    private bool _isShaking;
    
    #endregion
    
    
    
    #region ClassMethods

    protected override void Interact()
    {
        base.Interact();
        ChopTree();
    }

    protected override void InitInstanceData(InteractableSaveData saveData)
    {
        instanceData = new TreeSaveData(saveData.identifier.id);
        instanceData.instanceID = GenerateID();
    }

    public override void OnTileLoad(WorldTile loadedTile)
    {
        base.OnTileLoad(loadedTile);
        if(InstanceData.isChopped) RemoveLeaves();
    }
    
     private void ChopTree()
     {
         if (_isShaking) return;
         InstanceData.health--;
         Debug.Log("Hp left " + InstanceData.health);
         if (InstanceData.isChopped)
         {
             if (InstanceData.health > 0)
             {
                 StartCoroutine(RootActionDelay(0.75f));
             }
             else
             {
                 Destroy();
             }
         }
         else
         {
             if (InstanceData.health > 0)
             {
                 StartCoroutine(Shake(0.75f, 15f));
             }
             else 
             {
                 _fader.FadeIn();
                 StartCoroutine(Fall(2.5f, 
                     transform.position.x - GameObject.FindWithTag("Player").transform.position.x));
             }
         }
         
     }

     private void RemoveLeaves()
     {
         if(_fader is not null) Destroy(_fader.gameObject);
     }
     
     #endregion



     #region Coroutines

     private IEnumerator Fall(float duration, float direction)
     {
         InstanceData.health = 3;
         InstanceData.isChopped = true;
         _fader.IsBlocked = true;
         float t = 0.0f;
         while ( t  < duration )
         {
             t += Time.deltaTime;
             _fader.transform.rotation = Quaternion.AngleAxis
             (t / duration * 85f, 
                 direction < 0 ? Vector3.forward : Vector3.back);
             yield return null;
         }

         RemoveLeaves();
     }

     private IEnumerator Shake(float duration, float speed)
     {
         _isShaking = true;
         float t = 0.0f;
         while ( t  < duration )
         {
             t += Time.deltaTime;
             _fader.transform.rotation  = Quaternion.AngleAxis(Mathf.Sin(t * speed), Vector3.forward);
             yield return null;
         }
         _isShaking = false;
     }

     private IEnumerator RootActionDelay(float duration)
     {
         _isShaking = true;
         float t = 0.0f;
         while (t < duration)
         {
             t += Time.deltaTime;
             yield return null;
         }
         _isShaking = false;
     }

     #endregion
}