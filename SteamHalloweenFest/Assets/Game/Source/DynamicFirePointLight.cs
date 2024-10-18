using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DynamicFirePointLight : MonoBehaviour
{
    [SerializeField] private Light fireLight;   
    [SerializeField] private float minIntensity ;  
    [SerializeField] private float maxIntensity;  
    [SerializeField] private float minRange;        
    [SerializeField] private float maxRange;        
    [SerializeField] private float flickerSpeed;  

    private float _targetIntensity;
    private float _targetRange;

    void Start()
    {
        if (fireLight == null)
        {
            fireLight = GetComponent<Light>();
        }

        // Инициализация первых целевых значений
        _targetIntensity = Random.Range(minIntensity, maxIntensity);
        _targetRange = Random.Range(minRange, maxRange);
    }

    void Update()
    {
        // Постепенное изменение интенсивности
        fireLight.intensity = Mathf.Lerp(fireLight.intensity, _targetIntensity, flickerSpeed * Time.deltaTime);

        // Постепенное изменение радиуса света
        fireLight.range = Mathf.Lerp(fireLight.range, _targetRange, flickerSpeed * Time.deltaTime);

        // Когда достигается целевое значение, выбираем новое случайное значение
        if (Mathf.Abs(fireLight.intensity - _targetIntensity) < 0.05f)
        {
            _targetIntensity = Random.Range(minIntensity, maxIntensity);
        }

        if (Mathf.Abs(fireLight.range - _targetRange) < 0.05f)
        {
            _targetRange = Random.Range(minRange, maxRange);
        }
    }
}
