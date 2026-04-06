using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityMCPServer.Handlers;

public class TransparentDetection : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float transparencyAmount = 0.8f;
    [SerializeField] private float fadeTime = .4f;

    private SpriteRenderer spriteRenderer;
    private Tilemap tileMap;

    private Coroutine transparentCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        tileMap = GetComponent<Tilemap>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsLocalPlayer(collision.gameObject))
        {
            if (spriteRenderer)
            {
                transparentCoroutine = StartCoroutine(FadeRoutine(spriteRenderer, fadeTime, spriteRenderer.color.a, transparencyAmount));
            }
            else if (tileMap)
            {
                transparentCoroutine = StartCoroutine(FadeRoutine(tileMap, fadeTime, tileMap.color.a, transparencyAmount));   
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (IsLocalPlayer(collision.gameObject))
        {
            StopCoroutine(transparentCoroutine);
            if (spriteRenderer)
            {
                spriteRenderer.color = GenerateColorForAlphaValue(spriteRenderer, 1f);
            }
            else if (tileMap)
            {
                tileMap.color = GenerateColorForAlphaValue(tileMap, 1f);
            } 
        }
    }

    private bool IsLocalPlayer(GameObject hitGameObject) {
        // The local player will have a CapsuleCollider on the root gameobject
        NetworkObject networkObject = hitGameObject.GetComponent<NetworkObject>();
        CapsuleCollider2D capsuleCollider2D = hitGameObject.GetComponent<CapsuleCollider2D>();
        PlayerController playerController = hitGameObject.GetComponent<PlayerController>();

        return networkObject != null
            && capsuleCollider2D != null
            && playerController != null
            && networkObject.HasInputAuthority;
    }

    private IEnumerator FadeRoutine(SpriteRenderer spriteRenderer, float fadeTime, float previousAlphaValue, float targetAlphaValue)
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float newAlphaTransparency = Mathf.Lerp(previousAlphaValue, targetAlphaValue, elapsedTime / fadeTime);
            spriteRenderer.color = GenerateColorForAlphaValue(spriteRenderer, newAlphaTransparency);
            yield return null;
        }
    }
    
    private IEnumerator FadeRoutine(Tilemap tilemap, float fadeTime, float previousAlphaValue, float targetAlphaValue)
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float newAlphaTransparency = Mathf.Lerp(previousAlphaValue, targetAlphaValue, elapsedTime / fadeTime);
            tilemap.color = GenerateColorForAlphaValue(tilemap, newAlphaTransparency);
            yield return null;
        }
    }

    private static Color GenerateColorForAlphaValue(SpriteRenderer spriteRenderer, float newAlphaTransparency)
    {
        return new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlphaTransparency);
    }
    private static Color GenerateColorForAlphaValue(Tilemap tilemap, float newAlphaTransparency)
    {
        return new Color(tilemap.color.r, tilemap.color.g, tilemap.color.b, newAlphaTransparency);
    }
}
