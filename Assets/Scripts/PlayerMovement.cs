using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //----------------------- Var Extra -------------------------

    private Rigidbody2D rb;
    private Animator _anim;

    //-----------------------------------------------------------
    [Header("Inputs")]
    [SerializeField] private LayerMask _capaSuelo;
    private bool _enSuelo, _enPared, _enParedD, _nParedI;
    [SerializeField] private bool _puedeSaltar, _puedeHacerDash = true;
    private float _gravedadInicial;
    private bool _sePuedeMover = true;

    [SerializeField] private Transform _transformSuelo, _transformParedI, _transformParedD;
    [SerializeField] private Vector2 _tamañoCajaSuelo, _tamañoCajaPared;

    [Header("Movimiento")]
    [SerializeField] private float _velocidadMovimiento;
    [Range(0, 0.3f)] public float _suavisadoMovimiento;
    private Vector3 _velocidadZero = Vector3.zero;
    private bool _mirarDerecha = true;
    private bool _agachado = false;

    [Header("Salto")]
    [SerializeField] private float _fuerzaSalto;
    [SerializeField] private float _fuerzaSaltoParedX, _fuerzaSaltoParedY;
    [SerializeField] private float _tiempoSaltoPared;
    [SerializeField] private float _tiempoCoyoteTime, _tiempoGuardadoSalto;
    private bool _saltandoEnPared = false;
    private float _dirSaltoPared;
    private bool _saltoGuardado = false;
    private bool _cayendo = false;

    [Header("Dash")]
    [SerializeField] private float _velocidadDash;
    [SerializeField] private float _tiempoDash;

    [Header("Trepar")]
    private bool _agarrarPared, _primerToque = true, _deslizandose;
    [SerializeField] private float _velocidadDeTrepado;
    [SerializeField] private float _velocidadDeDeslizamiento;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _gravedadInicial = rb.gravityScale;
    }

    private void Update()
    {
        DetectarEstado();
        GetInputs();
    }

    private void FixedUpdate()
    {
        DetectarEntorno();
        // Moverse
        if (_sePuedeMover)
            Moverse(Input.GetAxisRaw("Horizontal") * Time.fixedDeltaTime);

        // Trepar
        if (_agarrarPared)
            Trepar(Input.GetAxisRaw("Vertical") * Time.fixedDeltaTime);
    }

    private void GetInputs()
    {
        // Saltar
        if ((Input.GetButtonDown("Jump")) && _puedeSaltar)
        {
            Saltar();
        }
        else if ((Input.GetButtonDown("Jump")) && !_puedeSaltar)
        {
            StartCoroutine(GuardarSalto());
        }

        // Agacharse
        if (Input.GetAxisRaw("Vertical") < 0 && _agachado == false && !_agarrarPared)
            Agacharse(true);
        else if (Input.GetAxisRaw("Vertical") >= 0 && _agachado == true)
            Agacharse(false);

        // Atacar
        if (Input.GetButtonDown("Fire1"))
            Atacar();

        // Dash
        if (Input.GetButtonDown("Fire3") && _puedeHacerDash)
            StartCoroutine(Dash());

        // Agarrar Pared
        if (_enPared && Input.GetButton("Fire2") && !_saltandoEnPared && _primerToque)
            AgarrarPared(true);
        else if (Input.GetButtonUp("Fire2"))
            AgarrarPared(false);

        if (Input.GetAxisRaw("Horizontal") != 0 && rb.velocity.y < 0 && _enPared && !_agarrarPared && !_saltandoEnPared)
            Deslizarse(true);
        else if (_deslizandose)
            Deslizarse(false);
    }

    private void DetectarEntorno()
    {
        _enSuelo = Physics2D.OverlapBox(_transformSuelo.position, _tamañoCajaSuelo, 0f, _capaSuelo);
        _nParedI = Physics2D.OverlapBox(_transformParedI.position, _tamañoCajaPared, 0f, _capaSuelo);
        _enParedD = Physics2D.OverlapBox(_transformParedD.position, _tamañoCajaPared, 0f, _capaSuelo);
    }

    private void DetectarEstado()
    {
        if (_nParedI || _enParedD)
            _enPared = true;
        else
        {
            _enPared = false;
            if (_agarrarPared)
                AgarrarPared(false);
        }

        if (_enSuelo)
        {
            _puedeHacerDash = true;
            if (_cayendo)
            {
                _cayendo = false;
                _anim.SetTrigger("Aterrizar");
            }
        }

        if (_enSuelo || _enPared)
        {
            _puedeSaltar = true;
            if (_saltoGuardado)
            {
                _saltoGuardado = false;
                Saltar();
            }
        }
        else
        {
            if (rb.velocity.y < 0 && _cayendo == false)
            {
                _cayendo = true;
            }
            StartCoroutine(CoyoteTime());   //Guardar salto aqui
        }
    }

    private void Moverse(float inputX)
    {
        float movimiento = inputX * _velocidadMovimiento;

        if (movimiento != 0)
            _anim.SetBool("Caminar", true);
        else
            _anim.SetBool("Caminar", false);

        if (!_saltandoEnPared)
        {
            Vector3 velocidadFinal = new Vector2(movimiento, rb.velocity.y);
            rb.velocity = Vector3.SmoothDamp(rb.velocity, velocidadFinal, ref _velocidadZero, _suavisadoMovimiento);
        }

        //Cambiar la direccion a la que saltará cuando este en una pared (Antes de girar)
        DireccionDeSalto();

        //Girar
        if ((movimiento > 0 && !_mirarDerecha) || (movimiento < 0 && _mirarDerecha))
            Girar();

    }

    private void Girar()
    {
        _mirarDerecha = !_mirarDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    private void DireccionDeSalto()
    {
        if (_nParedI)
            _dirSaltoPared = transform.localScale.x;
        else if (_enParedD)
            _dirSaltoPared = -1 * transform.localScale.x;
    }

    private string TipoDeSalto()
    {
        if ((_enSuelo && _enPared) || _enSuelo)
            return "Basico";
        else if (_enPared)
            return "Pared";
        else
            return "Basico";
    }

    private void Saltar()
    {
        AgarrarPared(false);
        _anim.SetTrigger("Saltar");

        switch (TipoDeSalto())
        {
            case "Basico":
                rb.AddForce(new Vector2(0, _fuerzaSalto));
                break;
            case "Pared":
                StartCoroutine(SaltandoPared());
                Girar();
                rb.velocity = new Vector2(_fuerzaSaltoParedX * _dirSaltoPared, _fuerzaSaltoParedY);
                break;
        }
    }

    IEnumerator GuardarSalto()
    {
        _saltoGuardado = true;
        yield return new WaitForSeconds(_tiempoGuardadoSalto);
        _saltoGuardado = false;
    }

    IEnumerator CoyoteTime()
    {
        yield return new WaitForSeconds(_tiempoCoyoteTime);
        _puedeSaltar = false;
    }

    IEnumerator SaltandoPared()
    {
        _saltandoEnPared = true;
        yield return new WaitForSeconds(_tiempoSaltoPared);
        _saltandoEnPared = false;
    }

    private IEnumerator Dash()
    {
        Debug.Log("Dash");

        _anim.SetBool("Dash", true);
        _puedeHacerDash = false;
        _sePuedeMover = false;
        rb.gravityScale = 0;
        rb.velocity = new Vector2(_velocidadDash * transform.localScale.x, 0);

        yield return new WaitForSeconds(_tiempoDash);
        _anim.SetBool("Dash", false);
        _sePuedeMover = true;
        rb.gravityScale = _gravedadInicial;
    }

    private void Agacharse(bool e)
    {
        Debug.Log("Agacharse:" + e);
        _agachado = e;
        string t = e ? "Agacharse" : "Pararse";
        _anim.SetTrigger(t);
    }

    private void Atacar()
    {
        Debug.Log("Atacar");
    }

    private void Deslizarse(bool deslizandose)
    {
        _deslizandose = deslizandose;
        if (_deslizandose)
        {
            rb.gravityScale = _velocidadDeDeslizamiento;
            deslizandose = true;
            _anim.SetTrigger("EnPared");
        }
        else
        {
            rb.gravityScale = _gravedadInicial;
            deslizandose = false;
        }
    }

    private void AgarrarPared(bool agarrarPared)
    {
        _agarrarPared = agarrarPared;
        _primerToque = true;

        if (_agarrarPared)
        {
            print("Trepando");
            _primerToque = false;
            rb.gravityScale = 0;
            _sePuedeMover = false;
            rb.velocity = new Vector2(0, 0);
        }
        else
        {
            print("Dejó de trepar");
            rb.gravityScale = _gravedadInicial;
            _sePuedeMover = true;
        }
    }

    private void Trepar(float inputY)
    {
        if (inputY == 0)
            _anim.SetTrigger("AgarrarPared");
        else if (inputY > 0)
            _anim.SetBool("Trepar", true);
        else if (inputY < 0)
            _anim.SetBool("Trepar", true);

        float movimiento = inputY * _velocidadDeTrepado;
        rb.velocity = new Vector2(0, movimiento);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_transformSuelo.position, _tamañoCajaSuelo);
        Gizmos.DrawWireCube(_transformParedD.position, _tamañoCajaPared);
        Gizmos.DrawWireCube(_transformParedI.position, _tamañoCajaPared);
    }
}