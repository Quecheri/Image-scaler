.code

ScaleDown proc
;Clear xmm1 register
vpxor xmm1,xmm1,xmm1
;Clear xmm0 register
vpxor xmm0,xmm0,xmm0

;RCX contains adress Int table in memory

;Load RGB Values from First pixel
;4th value is R value of second pixel
;which is ommited

movdqu xmm0,[RCX]

;Load RGB Values from Second pixel
;4th value is R value of third pixel
;which is ommited

movdqu xmm1,[RCX+12] 

;Add R1+R2 G1+G2 B1+B2
;And returns to xmm0

PADDD xmm0, xmm1

;Shift every value in xmm0 left once
;(R1+R2)/2 (G1+G2)/2 (B1+B2)/2

PSRLD xmm0, 1 

;saves results to memory (table)
;starting values get replaced
movdqu [RCX],xmm0


;Load  R3|G3|B3|{R4}
movdqu xmm0,[RCX+24]
;Load R4|G4|B4|{R5}
movdqu xmm1,[RCX+36]
;R4+R3|G4+G3|B4+B3|{R4+R5}
PADDD xmm0, xmm1
;(R3+R4)/2 |(G3+G4)/2 |(B3+B4)/2 |{(R4+R5)/2}
PSRLD xmm0, 1 
;Saves to memory new RGB values
movdqu [RCX+24],xmm0

;Load  R5|G5|B5|{R6}
movdqu xmm0,[RCX+48]
;Load R6|G6|B6{R7}
movdqu xmm1,[RCX+64]
;R5+R6|G5+G6|B5+B6|{R6+R7}
PADDD xmm0, xmm1
;(R5+R6)/2 |(G5+G6)/2 |(B5+B6)/2 |{(R6+R7)/2}
PSRLD xmm0, 1
;Saves to memory new RGB values
movdqu [RCX+48],xmm0

;Load  R7|G7|B7|{R8}
movdqu xmm0,[RCX+72]
;Load R8|G8|B8{#}
movdqu xmm1,[RCX+96]
;R7+R8|G7+G8|B7+B8|{R8+#}
PADDD xmm0, xmm1
;(R7+R8)/2 |(G7+G8)/2 |(B7+B8)/2 |{(R7+#)/2}
PSRLD xmm0, 1
;Saves to memory new RGB values
movdqu [RCX+72],xmm0



ret
ScaleDown endp
end