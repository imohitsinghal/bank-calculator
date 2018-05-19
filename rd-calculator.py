monthly = 5000;
time = 12;
rate = 6.5/100;
compounded = 4;
derivetime = time;

duration = 10;#years

def rdcomplete(mo,ti,ra,co):
   result = 0
   for i in range(0,ti):
    derivetime = ti - i;
    result = result+   rom(mo,derivetime,ra,co);
    if i >= ti:
        break;
   return result;
   
def rom(m,t,r,c):
   rom = m * (1+(r/c))**(c*float(t)/float(12));
   return rom;
   

rdamount = rdcomplete(monthly,time,rate,compounded)
print (rdamount);