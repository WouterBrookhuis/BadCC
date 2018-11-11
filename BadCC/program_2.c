int inner_loop(int ans, int i)
{
	for(int j = 20; j > 0; j--)
		if(i % 2 == 0)
			break;
		else
			ans = ans + i;
	return ans;
}

int main() {
	int ans = 0;
	for(int i = 0; i < 10; i++)
	{
		ans = inner_loop(ans, i);
	}
	return ans;
}