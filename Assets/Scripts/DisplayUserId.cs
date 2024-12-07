using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayUserId : MonoBehaviour
{
    public Text userId;
    PhotonView pv = null;
    // Start is called before the first frame update
    void Start()
    {
        pv = GetComponent<PhotonView>(); //포톤뷰 컴포넌트 정보를 얻어옴
        userId.text = pv.Owner.NickName; //userId 텍스트에 포톤 닉네임 입력
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
