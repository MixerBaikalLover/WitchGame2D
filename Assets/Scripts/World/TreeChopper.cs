using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeChopper : MonoBehaviour
{
    private Fader _fader;
    private int _hp = 2;
    private bool _isShaking;
    private bool _isChopped;

    private void OnMouseEnter()
    {
        _fader = transform.parent.GetComponentInChildren<Fader>();
        if (_fader is null) return;
        if(_fader.IsFaded) _fader.FadeIn();
    }

    private void OnMouseExit()
    {
        if (_fader is null) return;
        if(_fader.IsFaded) _fader.FadeOut();
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
        {
            ChopTree();
        }
    }

    private void ChopTree()
    {
        if (_isShaking || _isChopped) return;
        _hp--;
        Debug.Log("Hp left " + _hp);
        if (_hp > 0)
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
    
    private IEnumerator Fall(float duration, float direction)
    {
        _isChopped = true;
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

        Destroy(_fader.gameObject);
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
}
