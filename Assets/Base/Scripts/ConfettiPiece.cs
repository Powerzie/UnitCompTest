using UnityEngine;
using UnityEngine.UI;

public class ConfettiPiece : MonoBehaviour
{
    Vector2 _velocity;
    float _spin;
    float _life;
    Image _image;
    RectTransform _rt;

    public void Kick()
    {
        _rt = (RectTransform)transform;
        _image = GetComponent<Image>();
        _velocity = new Vector2(Random.Range(-240f, 240f), Random.Range(-420f, -180f));
        _spin = Random.Range(-420f, 420f);
        _life = Random.Range(3.2f, 4.8f);
    }

    void Update()
    {
        _velocity.y -= 980f * Time.deltaTime;
        _rt.anchoredPosition += _velocity * Time.deltaTime;
        _rt.Rotate(0f, 0f, _spin * Time.deltaTime);
        _life -= Time.deltaTime;
        if (_image != null && _life < 0.45f)
        {
            Color color = _image.color;
            color.a = Mathf.Clamp01(_life / 0.45f);
            _image.color = color;
        }

        if (_life <= 0f)
            Destroy(gameObject);
    }
}
